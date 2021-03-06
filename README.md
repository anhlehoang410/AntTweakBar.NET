AntTweakBar.NET 0.6.2
=====================

AntTweakBar.NET is an MIT-licensed C# wrapper for Philippe Decaudin's [AntTweakBar](http://anttweakbar.sourceforge.net) C/C++ GUI library. It allows C# developers to enhance their tech demos or games with an easy-to-use graphical widget for modifying application parameters in realtime. AntTweakBar.NET offers a high-level interface to the widget which will feel natural to any C# programmer, and also provides access to exception-safe bindings to the native AntTweakBar calls for those who might want them.

AntTweakBar.NET runs on the Microsoft .NET Framework 4 and on the Mono runtime, both 32-bit and 64-bit (provided it can find the appropriate AntTweakBar DLL or shared library). It has been tested on Windows and Linux, and is expected to work - but has not yet been tested - on Mac OS X and presumably BSD. The library is currently CLS-compliant and so should be able to be used from any .NET language, but this has not yet been verified.

License
-------

The AntTweakBar.NET wrapper is distributed under the MIT license, while AntTweakBar itself by Philippe Decaudin is released under the zlib/libpng license. For more information on licensing of this software, please consult the LICENSE file.

Installation
------------

For Linux, go to the [AntTweakBar SourceForge page](http://anttweakbar.sourceforge.net/doc/tools:anttweakbar:download) and download the native library, and simply `make & make install` as usual. You can then use the managed AntTweakBar.NET library, either by compiling it yourself or grabbing it from the [latest release](https://github.com/TomCrypto/AntTweakBar.NET/releases) or from [NuGet](https://www.nuget.org/packages/AntTweakBar.NET/).

For Windows, you have two options (the first one is easier, the second one more flexible):

- Use the standalone AntTweakBar.NET assembly, which you can compile from scratch using the standalone configurations, or can grab from the [latest release](https://github.com/TomCrypto/AntTweakBar.NET/releases) or from [NuGet](https://www.nuget.org/packages/AntTweakBar.NET.Standalone/). This assembly contains the native DLL's and unpacks it to the current directory as needed, which means it is larger in size and does some I/O work on startup, but is easier to use. Also, you cannot run a 32-bit and a 64-bit application from the same directory at the same time both using AntTweakBar.NET if using the standalone, and the unpacking code has a race condition if the DLL hasn't been unpacked yet (this should not be a problem).

- Go to the [AntTweakBar SourceForge page](http://anttweakbar.sourceforge.net/doc/tools:anttweakbar:download) and download the native library and then either install it on your system or add it to your project's build system. **Important**: AntTweakBar.NET will only ever look for `AntTweakBar.dll` for consistency reasons with other operating systems, so your build system must copy the right 32-bit or 64-bit native DLL to your project's output directory depending on the build configuration. If you are only interested in 64-bit, just discard the 32-bit native library, rename the 64-bit one by removing the "64" suffix, and use that one.

You're good to go! Please note the standalone build is designed more towards convenience for testing out the wrapper or for small applications, and is just dead weight compared to the normal build on any operating system besides Windows. It is assumed that sufficiently advanced projects will have some kind of build system in which they can integrate 32-bit and 64-bit native (unmanaged) libraries.

Quick Start
-----------

The AntTweakBar.NET high-level interface is divided into four main concepts: contexts, bars, variables and groups.

- **Context**: An instance of this class conceptually maps to a graphical window in your application: each window that will contain bars should have its own context. Each context holds its own separate set of bars, and has several methods to send window events to AntTweakBar, and, of course, draw the bars into the window.

- **Bar**: An instance of this class represents a graphical bar which holds a set of variables. It has several useful properties to tweak the bar to your application's needs. Each bar belongs to a context passed to its constructor.
 
- **Variable**: This is the base class from which all other variable types (like `IntVariable` or `StringVariable`) descend from. Just like the `Bar` class, it and its descendants have plenty of properties you can modify to tweak the variable's behavior and graphical appearance. In addition, value variables hold a value property which can be set graphically by the user and can be read on the fly by your code, this is the `Value` property for simple types (like `IntVariable`) or e.g. the `X`, `Y`, `Z` properties for the `VectorVariable`. They also have a `Changed` event to be notified when the user changes the variable's value. The `Button` variable type has a `Clicked` event instead. Each variable belongs to a bar passed to its constructor.

- **Group**: These are used to put a set of variables together in the bar for easy access, you can open (expand) or close (collapse) groups, and can put groups into groups for a hierarchical organization. Please see the "groups" page in the [wiki](https://github.com/TomCrypto/AntTweakBar.NET/wiki) to find out how to use them. 

The first context created should be passed the graphics API you are using, which is some version of OpenGL or DirectX. For DirectX, you must also pass a pointer to the native device, which should be available from the graphics framework you are using somehow (for instance, for SharpDX with Direct3D11, use `SharpDX.Direct3D11.Device.NativePointer`).

```csharp
using AntTweakBar;

/* ... */

context = new Context(Tw.GraphicsAPI.Direct3D11, /* pointer to device */);
/* or */
context = new Context(Tw.GraphicsAPI.OpenGL /* or OpenGLCore */);
```

Other contexts do not have to provide a graphics API, and can be created as simply `new Context();`. AntTweakBar.NET keeps track of how many contexts are active, initializes the AntTweakBar library whenever a first one is created, and terminates the library whenever the last one is destroyed.

Once you have a context, you can create bars inside it, and you can create variables inside these bars. To draw the context, call its `Draw()` method at the very end of your rendering pipeline. To handle events, hook up the various `Handle*()` methods to your window events. Keep in mind that you generally do not need to keep references to variables around. In many cases, it is sufficient to set up a delegate on the variable's `Changed` event to automatically modify some property in another class, so that your program automatically responds to variable changes.

```csharp
var myBar = new Bar(context);
myBar.Label = "Some bar";
myBar.Contained = true; // set some bar properties

var rotationVar = new IntVariable(myBar, 42 /* default value */);
rotationVar.Label = "Model rotation";
rotationVar.Changed += delegate { model.Rotation = rotationVar.Value; };

/* don't need rotationVar anymore (it will still be held onto by myBar) */
```

Generic event handling example to illustrate (this is not the only event you need to handle):

```csharp
protected override void OnResize(EventArgs e)
{
    base.OnResize(e);
    context.HandleResize(this.ClientSize);
}
```

The preferred way of doing event handling is by using the `Handle*()` events, which means you have to do some event translation. However, if you are using a particular framework, it may be possible to use ready-made event handlers. For instance, if you are using WinForms and happen to have access to your form's `WndProc` (perhaps because you are already overriding it) then you can use `EventHandlerWin()` to handle all events except perhaps `HandleResize` in a single line of code. There is currently such support for WinForms, SFML (via SFML.Net), X11 events and SDL (untested). The other two handlers supported by AntTweakBar (GLFW and GLUT) use per-event callbacks, so it probably does not make much sense wrapping them as your respective GLFW or GLUT wrapper should already convert them into events or delegates for you. Using the generic handlers works anywhere, though.

In general you *do* want to keep references to contexts, because you actually do need to destroy them when you close your windows. The different AntTweakBar.NET classes implement the `IDisposable` interface. When you dispose a bar, all variables inside it are implicitly disposed. When you dispose a context, all bars inside it are implicitly disposed. In other words, it is sufficient to dispose the contexts you create. It is very important to note that you must dispose the last context **before** terminating your graphics API. A symptom of failing to do this is an exception on shutdown pointing to the `Tw.Terminate()` function. Critically, this means you cannot just leave the contexts to be garbage-collected, as it will probably be too late by the time they are. This should not be a problem in most sensible implementations.  

For more information, make sure to check out the [wiki](https://github.com/TomCrypto/AntTweakBar.NET/wiki) (it's not finished, but already has some helpful content).

Notes on the Samples
--------------------

This repository contains a few samples, which are intended to show how to use the AntTweakBar.NET library with various graphics frameworks. There are currently two samples for the following technologies:

- SharpDX (a D3D11 sample lifted from SharpDX's MiniTri sample with some modifications)
- OpenGL (a fully fledged Newton fractal renderer with AntTweakBar widget controls)

These use the NuGet packages of their respective graphics frameworks. If you have compile errors, try a package restore. The OpenGL sample should be cross-platform, the SharpDX one is of course Windows-only. See `Samples/README.md` for more information on each.

Contribute
----------

Any issues or pull requests are welcome, I especially need help with verifying multi-window support, thread safety, and OS X testing, but any contribution is greatly appreciated. Thanks to *Ilkka Jahnukainen* for helping in testing AntTweakBar.NET throughout its ongoing development and providing valuable feedback to guide its design.

Changelog
---------

28 March 2015 (v0.6.2)

 - restructured project layout for multiple samples
 - moved sample readme stuff to the samples folder
 - reallowed (0, h) or (w, 0) as parameters to `HandleResize`

23 February 2015 (v0.6.1)

 - added first version of a simple DirectX11 sample (DX11Sample)

26 January 2015 (v0.6.0)

 - deprecated and removed mapping functionality
 - tagged v0.6.0 as a release with downloadable assemblies

15 January 2015 (v0.5.1)

 - added standalone configuration for Windows (embedded native DLL's for 32-bit and 64-bit)

20 December 2014 (v0.5.0)

 - changed `Tw.WindowSize` to accept sizes of (0, 0) to allow AntTweakBar resource cleanup (see [#3](https://github.com/TomCrypto/AntTweakBar.NET/issues/3))
 - added `ReleaseResources` and `ResetResources` methods to the `Context` class (see [#3](https://github.com/TomCrypto/AntTweakBar.NET/issues/3))
 - changed Sample to use GLSL version 120, fixed shader saving overwrite bug and close on Esc.
 - added TwDefineEnum and TwDefineStruct native functions and a DefineEnum low-level wrapper
 - various miscellaneous fixes and improvements to the Sample
 - added `Group` class and improved code relating to variable groups
 - added `StructVariable` abstract class
 - improved input handling (see [#4](https://github.com/TomCrypto/AntTweakBar.NET/issues/4))
 - added `ListVariable` variable type
 - added all missing bar properties

28 November 2014 (v0.4.4)

 - fixed an interop bug for 32-bit Windows (see [#1](https://github.com/TomCrypto/AntTweakBar.NET/issues/1))
 - changed Sample from using Tw.GraphicsAPI.OpenGLCore to Tw.GraphicsAPI.OpenGL for increased compatibility

26 November 2014 (v0.4.3)

 - added a few missing AntTweakBar functions in the low-level wrapper
 - added convenience Clear methods to bar and context
 - added MoveGroup method for nested groups

21 November 2014 (v0.4.2)

 - fixed a few more bugs in the sample
 - added ObjectDisposedException safety
 - sealed all classes and added validation events
 - updated to a new fancy readme file
 - removed the tutorial (the readme now has a quick start containing the same info)

20 November 2014 (v0.4.1)

 - bug fix release, fixed numerous bugs including:
   * string variable validation now works properly (previously threw exception instead of reverting)
   * groups names with spaces now work correctly
   * a few OnChanged methods were incorrectly private, they are now all public
   * unmanaged callbacks will now no longer be garbage-collected, which previously caused crashes
 - extended the sample with the ability to use symbolic variables in the fractal formula
 - extended the sample to be able to choose between hardcoding the fractal in the shader (efficient, but slow to update) and passing it as a uniform (a bit slower, but much faster to update)
 - fixed a bug in the polynomial class' Degree property

19 November 2014 (v0.4.0)

 - major refactoring, improvements and bug fixes
 - a few public changes, mostly renaming and moving things around
 - made the low-level wrapper API accessible to users in addition to the high-level classes
 - removed all unsafe code

18 November 2014 (v0.3.9)

 - tidied up project structure
 - fixed broken OpenTK references
 - added note about OpenTK references
 - renamed sample project window

18 November 2014 (v0.3.8)

 - uploaded wrapper to NuGet

4 September 2014 (v0.1.0)

 - added coordinate axes property
   (vectors/quaternions)

26 June 2014

 - added sample
 - many bug fixes
 - removed wrappers and redesigned composite types
   (colors/vectors/quaternions)

28 May 2014

 - added char overload for HandleKeyPress

24 May 2014

 - added wrappers for color/vector types

21 April 2014

 - first release
