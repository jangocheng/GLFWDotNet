# GLFWDotNet

.NET bindings for [GLFW](http://www.glfw.org).

## Usage

Use one of the following options:

* Compile the GLFWDotNet assembly using Visual Studio or MSBuild.
* Include [GLFW.cs](https://github.com/smack0007/GLFWDotNet/blob/master/Source/Library/GLFWDotNet/GLFW.cs) directly
  in your project.
  
The glfw dlls must be copied into x86/x64 subdirectories relative to the GLFWDotNet.dll. See the output of the samples. An example MSBuild Target can be seen in the [Samples.targets](https://github.com/smack0007/GLFWDotNet/blob/master/Source/Samples/Common/Samples.targets) file.

## Samples

There are a few [samples](https://github.com/smack0007/GLFWDotNet/tree/master/Source/Samples) showing how to use GLFWDotNet.

* [HelloWorld](https://github.com/smack0007/GLFWDotNet/tree/master/Source/Samples/HelloWorld) Basic HelloWorld program to get you started.
* [HelperClasses](https://github.com/smack0007/GLFWDotNet/tree/master/Source/Samples/HelperClasses) An example which shows the usage of some the included helper classes.
* [SharpBgfxIntegration](https://github.com/smack0007/GLFWDotNet/tree/master/Source/Samples/SharpBgfxIntegration) An example of how to use [SharpBgfx](https://github.com/MikePopoloski/SharpBgfx) with GLFWDotNet.