# Time Control :: Tech Notes

## Notes by Xaviier

### Difference between `update` and `fixedupdate`

[source](http://answers.unity3d.com/questions/10993/whats-the-difference-between-update-and-fixedupdat.html)

The only change from 0.23 to 0.23.5 is that a TimeWarp object now exists in the space center and tracking station, which means there's no reason to hack the warp in the tracking station now.

I'll give an explanation of everything I've learned about the internal systems so that others can utilize them in their mods or understand how they interact.

There are three main things that control how time progresses in KSP:

The Built-in Unity Time
A TimeWarp object
The Planetarium object

### The TimeWarp Object:

This object appears to be instantiated in the flight scene only, it handles all the physics and warping things for the **currently active vessel**. It can be accessed by grabbing a reference to it from the static definition. The only known documentation for accessing it is [here](https://github.com/net-lisias-ksp/XML-Documentation-for-the-KSP-API/blob/master/src/TimeWarp.cs). This is where warp-rates can be fussed with. I won't bother to repeat anything that is well documented on there already, so make sure to look at it. Note that the methods to get info are marked as extern, which probably means that they are just pointing to the standard unity time stuff. Controlling the physics simulation can be done by simply accessing the static variables as described and explained [here](https://docs.unity3d.com/Documentation/ScriptReference/Time.html).

What is important to know, and which very few understand, is how Unity handles time passage internally.

There are three important values:

* **timeScale** - this is the rate of passage of time, typically 1x, changing this is how phys-warp works.
* **fixedDeltaTime** - essentially the "accuracy" of physics, typically is a value of 0.02, though activating phys-warp multiplies it by the rate (to keep the same number of physics steps per second the same), which is why we see lower accuracy in phys-warp.
* **maxDeltaTime** - this is largely tied into how unity does its automatic slow-down to account for intense physics calculation instead of lowering FPS, this is the same value as you can find in the settings menu.
Something to note: this must be >= fixedDeltaTime, but KSP (or perhaps Unity) internally doesn't bother to put it back down if you exceed it. If you set your maxDeltaTime to say 0.03, and then use phys-warp, you will find it goes up to 0.08 and stays there, which is why phys warping before launching a vessel might cause a faster time rate, but worse FPS. 

### The Planetarium Object:

This object exists throughout the game, and is basically the entire universe, planets, ships on rails, etc, except for your active vessel (if you are in the flight scene). The only known documentation for accessing it is [here](https://github.com/net-lisias-ksp/XML-Documentation-for-the-KSP-API/blob/master/src/Planetarium.cs). This is where altitude limits and the rate of time can be fussed with. Again, there is no need to explain what is already made obvious in the documentation.

There are two important variables in this object:

* **timeScale** - this is the warp rate, accessing this is how I did tracking station warping, and likely how the ARM update does that as well as space center warping. It seems to be automatically kept up to date when warping in the flight scene, but take care if you are changing it yourself.
* **fixedDeltaTime** - this is typically the same as the one in TimeWarp, though as I found out, you do need to make sure it matches the rate in TimeWarp if you do fiddle with that, otherwise the universe won't be moving at the same speed you are.

From some testing it doesn't appear to be necessary to modify timeScale as well when you do things like slow-mo, though I am not entirely sure. It's rather difficult to gauge a lot of this stuff. If I'm wrong, then the universe might still be getting screwed up by slow-mo.

## Notes by LisiasT

The notes from Xaiier were transcripted *ipsi-literis* and reflect what he had at hand at that time (2014). Broken links were fixed to a interim fork. 
