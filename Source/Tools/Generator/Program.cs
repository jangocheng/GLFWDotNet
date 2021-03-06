﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Generator
{
    class Program
    {
        interface IDocs
        {
            string BriefDescription { get; set; }

            string Description { get; set; }

            string Remarks { get; set; }

            string ReturnDescription { get; set; }

            string Group { get; set; }

            IEnumerable<KeyValuePair<string, string>> GetParamDocs();

            void SetParamDescription(string name, string description);

            void SetParamModifier(string name, ParamModifier modifier);
        }

        class EnumData : IDocs
        {
            public string BriefDescription { get; set; }

            public string Description { get; set; }

            public string Remarks { get; set; }

            public string ReturnDescription { get; set; }

            public string Group { get; set; }

            public string Name { get; set; }

            public string Value { get; set; }

            public override string ToString() => this.Name;

            public IEnumerable<KeyValuePair<string, string>> GetParamDocs()
            {
                throw new NotImplementedException();
            }

            public void SetParamDescription(string name, string description)
            {
                throw new NotImplementedException();
            }

            public void SetParamModifier(string name, ParamModifier modifier)
            {
                throw new NotImplementedException();
            }
        }

        class FunctionData : IDocs
        {
            public string Group { get; set; }

            public string BriefDescription { get; set; }

            public string Description { get; set; }

            public string Remarks { get; set; }

            public string ReturnDescription { get; set; }

            public string ReturnType { get; set; }

            public string Name { get; set; }

            public List<ParamData> Params { get; } = new List<ParamData>();

            public override string ToString() => this.Name;

            public IEnumerable<KeyValuePair<string, string>> GetParamDocs() =>
                this.Params.Select(x => new KeyValuePair<string, string>(x.Name, x.Description));

            public void SetParamDescription(string name, string description)
            {
                this.Params.Single(x => x.Name == name).Description = description;
            }

            public void SetParamModifier(string name, ParamModifier modifier)
            {
                this.Params.Single(x => x.Name == name).Modifier = modifier;
            }
        }

        enum ParamModifier
        {
            None,
            In,
            Out
        }

        class ParamData
        {
            public ParamModifier Modifier { get; set; }

            public string Description { get; set; }

            public string Type { get; set; }

            public string Name { get; set; }

            public override string ToString() => this.Name;
        }

        class CallbackData : IDocs
        {
            public string Group { get; set; }

            public string BriefDescription { get; set; }

            public string Description { get; set; }

            public string Remarks { get; set; }

            public string ReturnDescription { get; set; }

            public string ReturnType { get; set; }

            public string Name { get; set; }

            public List<ParamData> Params { get; } = new List<ParamData>();

            public override string ToString() => this.Name;

            public IEnumerable<KeyValuePair<string, string>> GetParamDocs() =>
                this.Params.Select(x => new KeyValuePair<string, string>(x.Name, x.Description));

            public void SetParamDescription(string name, string description)
            {
                // Abusing the fact that SetParamDescription is called first in order to
                // the name from the docs.
                var param = this.Params.First(x => x.Name == null);
                param.Name = name;
                param.Description = description;
            }

            public void SetParamModifier(string name, ParamModifier modifier)
            {
                var param = this.Params.First(x => x.Name == name).Modifier = modifier;
            }
        }

        class StructData
        {
            public bool IsOpaque { get; set; }

            public string Name { get; set; }

            public List<ParamData> Members { get; } = new List<ParamData>();

            public override string ToString() => this.Name;
        }

        public static void Main(string[] args)
        {
            var lines = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "glfw3.h"));
            var enums = new List<EnumData>();
            var functions = new List<FunctionData>();
            var callbacks = new List<CallbackData>();
            var structs = new List<StructData>();

            Parse(lines, enums, functions, callbacks, structs);

            Write(enums, functions, callbacks, structs);

            WriteEnumFile(
                enums.Where(x => x.Value.StartsWith("0x00022") || x.Value.StartsWith("0x00030") || x.Value.StartsWith("0x00031") || x.Value.StartsWith("0x00032") || x.Value.StartsWith("0x00035")
                                 || x.Name == "GLFW_NO_ROBUSTNESS" || x.Name == "GLFW_OPENGL_ANY_PROFILE" || x.Name == "GLFW_ANY_RELEASE_BEHAVIOR"),
                x =>
                {
                    return InflectEnumName(x.Substring("GLFW_".Length));
                },
                "ContextHints");

            WriteEnumFile(
                enums.Where(x => x.Value.StartsWith("0x00021")),
                x =>
                {
                    return InflectEnumName(x.Substring("GLFW_".Length));
                },
                "FramebufferHints");

            var keyEnums = enums.Where(x => x.Name.StartsWith("GLFW_KEY") && !x.Name.EndsWith("LAST"));

            Func<string, string> inflectKeyEnum = x =>
            {
                string name = InflectEnumName(x.Substring("GLFW_KEY".Length));

                if (name.StartsWith("_"))
                    name = name.Replace("_", "D");

                if (name.StartsWith("Kp"))
                    name = name.Replace("Kp", "KeyPad");

                return name;
            };

            WriteEnumFile(
                keyEnums,
                inflectKeyEnum,
                "Keys");

            WriteKeyboardMethods(keyEnums, inflectKeyEnum);

            WriteEnumFile(
                enums.Where(x => x.Name.StartsWith("GLFW_MOD")),
                x =>
                {
                    return InflectEnumName(x.Substring("GLFW_MOD".Length));
                },
                "KeyModifiers");
                       
            WriteEnumFile(
                enums.Where(x => x.Name.StartsWith("GLFW_JOYSTICK") && !x.Name.EndsWith("LAST")),
                x =>
                {
                    string name = InflectEnumName(x.Substring("GLFW_JOYSTICK".Length));

                    if (name.StartsWith("_"))
                        name = name.Replace("_", "Button");

                    return name;
                },
                "JoystickButtons");

            WriteEnumFile(
                enums.Where(x => x.Name.StartsWith("GLFW_MOUSE_BUTTON") && !x.Name.EndsWith("LAST")),
                x =>
                {
                    string name = InflectEnumName(x.Substring("GLFW_MOUSE_BUTTON".Length));

                    if (name.StartsWith("_"))
                        name = name.Replace("_", "Button");

                    return name;
                },
                "MouseButtons");

            WriteEnumFile(
                enums.Where(x => x.Value.StartsWith("0x00020")),
                x =>
                {
                    return InflectEnumName(x.Substring("GLFW_".Length));
                },
                "WindowHints");
        }

        private static string ParseType(string[] parts, ref int j)
        {
            string result = string.Empty;

            if (parts[j] == "const")
            {
                result += "const ";
                j++;
            }
            
            if (parts[j] == "unsigned")
            {
                result += "unsigned ";
                j++;
            }

            result += parts[j];

            return result;
        }

        private static FunctionData ParseFunction(string[] lines, int i)
        {
            string[] parts = lines[i].Split(new char[] { ' ', '\t', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);

            int j = 1;

            FunctionData function = new FunctionData();

            function.ReturnType = ParseType(parts, ref j);
            j++;

            function.Name = parts[j];
            j++;

            if (parts[j] != "void")
            {
                while (parts[j] != ";")
                {
                    ParamData paramData = new ParamData();

                    paramData.Type = ParseType(parts, ref j);
                    j++;

                    paramData.Name = parts[j];
                    j++;

                    function.Params.Add(paramData);
                }
            }

            ParseDocs(lines, i, function);

            return function;
        }

        enum DocsState
        {
            None,
            BriefDescription,
            Description,
            Param,
            Remarks,
            Return
        }

        private static void ParseDocsFlush(
            IDocs docs,
            ref DocsState state,
            StringBuilder sb,
            string paramName,
            ParamModifier paramModifier)
        {
            switch (state)
            {
                case DocsState.BriefDescription:
                    docs.BriefDescription = sb.ToString();
                    break;

                case DocsState.Description:
                    docs.Description = sb.ToString();
                    break;

                case DocsState.Param:
                    docs.SetParamDescription(paramName, sb.ToString());
                    docs.SetParamModifier(paramName, paramModifier);
                    break;

                case DocsState.Remarks:
                    docs.Remarks = sb.ToString();
                    break;

                case DocsState.Return:
                    docs.ReturnDescription = sb.ToString();
                    break;
            }

            if (state == DocsState.BriefDescription)
            {
                state = DocsState.Description;
            }
            else
            {
                state = DocsState.None;
            }

            sb.Clear();
        }

        private static void ParseDocs(string[] lines, int i, IDocs docs)
        {
            while (!lines[i].StartsWith("/*!"))
            {
                i--;
            }

            DocsState state = DocsState.None;
            StringBuilder sb = new StringBuilder(1024);
            string paramName = string.Empty;
            ParamModifier paramModifier = ParamModifier.None;

            while (true)
            {
                bool finished = false;

                string trimmedLine = null;

                if (lines[i].Length >= 4)
                {
                    trimmedLine = lines[i].Substring(4);
                }
                else
                {
                    trimmedLine = string.Empty;
                }

                if (trimmedLine.StartsWith("@brief"))
                {
                    state = DocsState.BriefDescription;
                    sb.AppendLine(trimmedLine.Substring("@brief ".Length));
                }
                else if (trimmedLine.StartsWith("@ingroup"))
                {
                    docs.Group = trimmedLine.Substring("@ingroup ".Length).Trim();
                }
                else if (trimmedLine.StartsWith("@param"))
                {
                    ParseDocsFlush(docs, ref state, sb, paramName, paramModifier);

                    state = DocsState.Param;
                    paramModifier = ParamModifier.None;

                    trimmedLine = trimmedLine.Substring("@param".Length);

                    if (trimmedLine.StartsWith("[in]"))
                    {
                        paramModifier = ParamModifier.In;
                        trimmedLine = trimmedLine.Substring("[in]".Length);
                    }
                    else if (trimmedLine.StartsWith("[out]"))
                    {
                        paramModifier = ParamModifier.Out;
                        trimmedLine = trimmedLine.Substring("[out]".Length);
                    }

                    trimmedLine = trimmedLine.Substring(1);

                    int index = trimmedLine.IndexOf(' ');
                    paramName = trimmedLine.Substring(0, index);

                    sb.AppendLine(trimmedLine.Substring(index + 1));
                }
                else if (trimmedLine.StartsWith("@remarks"))
                {
                    state = DocsState.Remarks;
                    sb.AppendLine(trimmedLine.Substring("@remarks ".Length));
                }
                else if (trimmedLine.StartsWith("@return"))
                {
                    ParseDocsFlush(docs, ref state, sb, paramName, paramModifier);
                    state = DocsState.Return;
                    sb.AppendLine(trimmedLine.Substring("@return ".Length));
                }
                else if (lines[i].StartsWith(" */"))
                {
                    ParseDocsFlush(docs, ref state, sb, paramName, paramModifier);
                    finished = true;
                }
                else if (trimmedLine == string.Empty || trimmedLine.StartsWith("@"))
                {
                    ParseDocsFlush(docs, ref state, sb, paramName, paramModifier);
                }
                else
                {
                    sb.AppendLine(trimmedLine);
                }

                if (finished)
                    break;

                i++;
            }
        }

        private static CallbackData ParseCallback(string[] lines, int i)
        {
            string[] parts = lines[i].Split(new char[] { ' ', '\t', '(', ')', ',' }, StringSplitOptions.RemoveEmptyEntries);

            int j = 1;

            CallbackData callback = new CallbackData();

            callback.ReturnType = ParseType(parts, ref j);
            j += 2;

            callback.Name = parts[j];
            j++;

            if (parts[j] != "void")
            {
                while (parts[j] != ";")
                {
                    callback.Params.Add(new ParamData() { Type = ParseType(parts, ref j) });
                    j++;
                }
            }

            ParseDocs(lines, i, callback);

            return callback;
        }

        private static StructData ParseStruct(string[] lines, ref int i)
        {
            string[] parts = lines[i].Split(' ');

            var @struct = new StructData();
            @struct.Name = parts[2];

            if (lines[i].EndsWith(";"))
            {
                @struct.IsOpaque = true;
            }
            else if (lines[i + 1].StartsWith("{"))
            {
                i++;
                while (!lines[i].StartsWith("}"))
                {
                    string line = lines[i].Trim();

                    if (line.EndsWith(";"))
                    {
                        parts = line.Substring(0, line.Length - 1).Split(' ');
                        int j = 0;

                        var member = new ParamData();

                        member.Type = ParseType(parts, ref j);
                        j++;

                        member.Name = parts[j];

                        @struct.Members.Add(member);
                    }

                    i++;
                }
            }

            return @struct;
        }

        private static void Parse(
            string[] lines,
            List<EnumData> enums,
            List<FunctionData> functions,
            List<CallbackData> callbacks,
            List<StructData> structs)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("#define "))
                {
                    string[] parts = lines[i].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 3)
                    {
                        var @enum = new EnumData()
                        {
                            Name = parts[1],
                            Value = parts[2]
                        };

                        enums.Add(@enum);

                        ParseDocs(lines, i, @enum);
                    }
                }
                else if (lines[i].StartsWith("GLFWAPI "))
                {
                    FunctionData function = ParseFunction(lines, i);
                    functions.Add(function);
                }
                else if (lines[i].StartsWith("typedef "))
                {
                    if (lines[i].Contains("(* "))
                    {
                        CallbackData callback = ParseCallback(lines, i);
                        callbacks.Add(callback);
                    }
                    else if (lines[i].StartsWith("typedef struct "))
                    {
                        StructData @struct = ParseStruct(lines, ref i);
                        structs.Add(@struct);
                    }
                }
            }
        }

        private static string ReplaceCommonWords(string input)
        {
            return input
                .Replace("bits", "Bits")
                .Replace("button", "Button")
                .Replace("close", "Close")
                .Replace("enter", "Enter")
                .Replace("focus", "Focus")
                .Replace("iconify", "Iconify")
                .Replace("mods", "Mods")
                .Replace("pos", "Pos")
                .Replace("refresh", "Refresh")
                .Replace("size", "Size");
        }

        private static string InflectGLFWName(string input)
        {
            if (input.StartsWith("glfw") || input.StartsWith("GLFW"))
            {
                input = input.Substring("GLFW".Length);
                input = input.Substring(0, 1).ToUpper() + input.Substring(1);

                if (input.EndsWith("fun"))
                {
                    input = input.Substring(0, input.Length - "fun".Length) + "Fun";
                    input = ReplaceCommonWords(input);
                }

                if (input == "Vidmode")
                {
                    input = "VidMode";
                }

                if (input == "Gammaramp")
                {
                    input = "GammaRamp";
                }
            }

            return input;
        }

        private static string GetType(string type, List<StructData> structs)
        {
            if (type.StartsWith("const "))
                type = type.Substring("const ".Length);

            if (type.EndsWith("*") || type.EndsWith("**"))
            {
                string structName = type.TrimEnd('*');

                var @struct = structs.SingleOrDefault(x => x.Name == structName);

                if (@struct != null)
                {
                    if (@struct.IsOpaque)
                    {
                        if (type.EndsWith("**"))
                        {
                            return "IntPtr[]";
                        }
                        else
                        {
                            return "IntPtr";
                        }
                    }
                    else
                    {
                        type = structName;
                    }
                }
            }

            type = InflectGLFWName(type);

            switch (type)
            {
                case "char*":
                    return "string";

                case "char**":
                    return "string[]";

                case "double*":
                    return "double";

                case "float*":
                    return "float[]";

                case "Glproc":
                    return "IntPtr";

                case "int*":
                    return "int";

                case "uint32_t":
                case "uint32_t*":
                    return "uint";

                case "uint64_t":
                    return "ulong";

                case "unsigned int":
                    return "uint";

                case "unsigned char*":
                    return "string";

                case "unsigned short*":
                    return "ushort[]";

                case "VkAllocationCallbacks*":
                case "VkInstance":
                case "VkPhysicalDevice":
                case "Vkproc":
                case "VkSurfaceKHR*":
                    return "IntPtr";

                case "VkResult":
                    return "int";

                case "void*":
                    return "IntPtr";
            }

            return type;
        }

        private static string GetReturnType(string type, List<StructData> structs)
        {
            return GetType(type, structs);
        }

        private static string GetParamType(string type, ParamModifier modifier, List<StructData> structs)
        {
            type = GetType(type, structs);

            if (modifier == ParamModifier.Out)
            {
                type = "out " + type;
            }

            return type;
        }

        private static string GetFunctionName(string function)
        {
            return InflectGLFWName(function);
        }

        private static string GetParamName(string name)
        {
            switch (name)
            {
                case "event":
                    return "@event";

                case "string":
                    return "@string";
            }

            return name;
        }

        private static string FormatDocs(string input, string padding)
        {
            return
                padding + "/// " +
                string.Join(
                    Environment.NewLine + padding + "/// ",
                    input.TrimEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                );
        }

        private static void WriteDocs(IDocs docs, StringBuilder sb, string padding, bool descriptionOnly = false)
        {
            sb.AppendLine($"{padding}/// <summary>");
            sb.AppendLine(FormatDocs(docs.Description, padding));
            sb.AppendLine($"{padding}/// </summary>");

            if (descriptionOnly)
                return;

            if (!string.IsNullOrEmpty(docs.Remarks))
            {
                sb.AppendLine($"{padding}/// <remarks>");
                sb.AppendLine(FormatDocs(docs.Remarks, padding));
                sb.AppendLine($"{padding}/// </remarks>");
            }

            foreach (var param in docs.GetParamDocs())
            {
                sb.AppendLine($"{padding}/// <param name=\"{param.Key}\">");
                sb.AppendLine(FormatDocs(param.Value, padding));
                sb.AppendLine($"{padding}/// </param>");
            }

            if (!string.IsNullOrEmpty(docs.ReturnDescription))
            {
                sb.AppendLine($"{padding}/// <returns>");
                sb.AppendLine(FormatDocs(docs.ReturnDescription, padding));
                sb.AppendLine($"{padding}/// </returns>");
            }
        }

        private static void Write(
            List<EnumData> enums,
            List<FunctionData> functions,
            List<CallbackData> callbacks,
            List<StructData> structs)
        {
            string[] license = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "License.txt"));

            StringBuilder sb = new StringBuilder(1024);

            foreach (var line in license)
            {
                sb.Append("// ");
                sb.AppendLine(line);
            }

            sb.AppendLine();

            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using System.Runtime.InteropServices;");
            sb.AppendLine("using System.Runtime.Loader;");
            sb.AppendLine("using System.Security;");

            sb.AppendLine();
            sb.AppendLine("namespace GLFWDotNet");
            sb.AppendLine("{");
            sb.AppendLine("\tpublic static partial class GLFW");
            sb.AppendLine("\t{");
            sb.AppendLine("\t\tprivate const string Library = \"glfw3\";");
            sb.AppendLine();

            foreach (var @enum in enums)
            {
                string name = @enum.Name.Substring(5);
                string value = @enum.Value;

                if (value.StartsWith("GLFW_"))                
                    value = value.Substring(5);

                sb.AppendLine($"\t\tpublic const int {name} = {value};");
            }

            sb.AppendLine();

            foreach (var @struct in structs.Where(x => !x.IsOpaque))
            {
                sb.AppendLine($"\t\tpublic struct {GetType(@struct.Name, structs)}");
                sb.AppendLine("\t\t{");

                foreach (var member in @struct.Members)
                {
                    var name = InflectEnumName(member.Name);
                    var type = GetParamType(member.Type, ParamModifier.None, structs);
                    sb.AppendLine($"\t\t\tpublic {type} {name};");
                }

                sb.AppendLine("\t\t}");

                sb.AppendLine();
            }

            foreach (var callback in callbacks)
            {
                WriteDocs(callback, sb, "\t\t");

                string parameters = string.Join(", ", callback.Params.Select(x => GetParamType(x.Type, x.Modifier, structs) + " " + GetParamName(x.Name)));

                sb.AppendLine("\t\t[UnmanagedFunctionPointer(CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]");
                sb.AppendLine($"\t\tpublic delegate {GetReturnType(callback.ReturnType, structs)} {GetFunctionName(callback.Name)}({parameters});");

                sb.AppendLine();
            }
                        
            foreach (var function in functions)
            {
                WriteDocs(function, sb, "\t\t");

                string parameters = string.Join(", ", function.Params.Select(x => GetParamType(x.Type, x.Modifier, structs) + " " + GetParamName(x.Name)));

                sb.AppendLine($"\t\t[DllImport(Library, EntryPoint = \"{function.Name}\", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]");
                sb.AppendLine($"\t\tpublic static extern {GetReturnType(function.ReturnType, structs)} {GetFunctionName(function.Name)}({parameters});");

                sb.AppendLine();
            }

            sb.AppendLine("\t\tprivate class GLFWAssemblyLoadContext : AssemblyLoadContext");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tinternal void Init()");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\tthis.LoadUnmanagedDllFromPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Environment.Is64BitProcess ? \"x64\" : \"x86\", Library));");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t\tprotected override Assembly Load(AssemblyName assemblyName) => null;");
            sb.AppendLine("\t\t}");
            sb.AppendLine();

            sb.AppendLine("\t\tstatic GLFW()");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tnew GLFWAssemblyLoadContext().Init();");
            sb.AppendLine("\t\t}");
            
            sb.AppendLine("\t}");
            sb.AppendLine("}");

            File.WriteAllText(@"..\..\Library\GLFWDotNet\GLFW.cs", sb.ToString());
        }

        private static string InflectEnumName(string input)
        {
            string[] parts = input.Split('_');
            string[] temp = new string[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    int capitalizeLength = 1;

                    if (parts[i].Length > 1 && char.IsDigit(parts[i][0]))
                        capitalizeLength = 2;

                    temp[i] = parts[i].Substring(0, capitalizeLength).ToUpper() + parts[i].Substring(capitalizeLength).ToLower();
                }
            }

            string name = string.Join(string.Empty, temp);

            if (char.IsDigit(name[0]))
                name = "_" + name;

            return ReplaceCommonWords(name);
        }

        private static void WriteEnumFile(
            IEnumerable<EnumData> enums,
            Func<string, string> inflectName,
            string enumName)
        {
            StringBuilder sb = new StringBuilder(1024);

            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace GLFWDotNet");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic enum {enumName}");
            sb.AppendLine("\t{");

            foreach (var @enum in enums)
            {
                string leftName = inflectName(@enum.Name);
                string rightName = @enum.Name.Substring(5);
                sb.AppendLine($"\t\t{leftName} = GLFW.{rightName},");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            File.WriteAllText($@"..\..\Library\GLFWDotNet\{enumName}.Generated.cs", sb.ToString());
        }

        private static void WriteKeyboardMethods(IEnumerable<EnumData> keyEnums, Func<string, string> inflectName)
        {
            StringBuilder sb = new StringBuilder(1024);

            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace GLFWDotNet");
            sb.AppendLine("{");
            sb.AppendLine($"\tpublic partial class Keyboard");
            sb.AppendLine("\t{");

            var keysToMap = keyEnums.Skip(1).ToArray();

            sb.AppendLine($"\t\tprivate readonly bool[] keyMap = new bool[{keysToMap.Length}];");
            sb.AppendLine();

            sb.AppendLine("\t\tprivate int GetKeyMapIndex(Keys key)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tswitch(key)");
            sb.AppendLine("\t\t\t{");

            for (int i = 0; i < keysToMap.Length; i++)
            {
                string name = inflectName(keysToMap[i].Name);
                sb.AppendLine($"\t\t\t\tcase Keys.{name}: return {i};");
            }

            sb.AppendLine("\t\t\t}");
            sb.AppendLine();

            sb.AppendLine("\t\t\treturn -1;");
            sb.AppendLine("\t\t}");

            sb.AppendLine("\t}");
            sb.AppendLine("}");

            File.WriteAllText($@"..\..\Library\GLFWDotNet\Keyboard.Generated.cs", sb.ToString());
        }
    }
}
