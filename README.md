# Threaded Cascade Classifiers for Unity with Vuforia

## Getting Started
For this script to work with Unity, you must compile a few dependencies. 
Don't be afraid, it's not that complicated!

> Preconditions:
- Microsoft Visual Studio
- Microsoft VC++ distributables
- Emgu CV use WCF(Windows Communication Foundation), therefore requires .Net 3.0

1. You will need EmguCV, a .net wrapper for OpenCV
  1. Download the newest release of EmguCV from https://sourceforge.net/projects/emgucv/
  2. Install EmguCV
  3. Open the root of EmguCV in your explorer and go to `/EmguCV/Solution/<platform>/`
  4. Open the solution `Emgu.CV.sln` inside the folder (You will need Microsoft Visual Studio, as well as the newest VC++ distributables for that)
  5. If your preconditions are all set, you should now be able to build the whole project by right clicking on the project in the solution and selecting the option `Build Solution`. This can also be done by selecting `Build > Build Solution`
  
2. Now you have to copy the compiled dependencies to your Unity project
  1. Create a new folder `Plugins` inside your Unity project `Assets` folder
  2. Go to `/EmguCV/libs` and copy all the dlls (with the already given structure), paste these dlls with the subfolders to your created `Plugins` directory. `Plugins` should now look like this: [Plugins after EmguCV dlls](https://raw.githubusercontent.com/Wurmloch/Unity-Threaded-CascadeClassifier/master/docs/emgucv_plugins.png)
  
3. For the classifiers, we also need the `Rectangle` class from `System.Drawing`. Since `System.Drawing` is not included in Unity by default (no support), we have to include the `System.Drawing` dll.
  1. Just navigate to your Unity installation directory and go to `Unity/Editor/Data/Mono/lib/mono/2.0`
  2. Copy the file `System.Drawing.dll`
  3. Paste it to your `Plugins` folder, just like before. `Plugins` should now look like this: [Plugins after System Drawing dlls](https://raw.githubusercontent.com/Wurmloch/Unity-Threaded-CascadeClassifier/master/docs/emgucv_plugins_drawing.png)
  
