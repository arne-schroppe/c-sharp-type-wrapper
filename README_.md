
# C# Type Wrapper Generator

How to install: Use the TypeWrapperSourceGenerator project to create the source generator dll. 
Add it together with "Types/Feature.cs" and "Types/TypeWrapperAttribute.cs" to your Unity project. 
See here for more information on [Roslyn Source Generators in Unity](https://docs.unity3d.com/2023.2/Documentation/Manual/roslyn-analyzers.html)

You can use the following syntax to get a type safe wrapper generated for your primitive types:

```c#
[TypeWrapper(typeof(string))]
readonly partial struct UserId
{
}
```

And then use it with
```c#
var userId = new UserId("JLP-47AT");
```

You can optionally get a `JsonConverter` generated, if you use NewtonSoft Json:

```c#
[TypeWrapper(typeof(string), Feature.NewtonSoftJsonConverter)]
readonly partial struct UserId
{
}
```

(There is also `Feature.UnitySerializable`.)

If you want to verify or manipulate the value, there's the `OnCreate` hook:

```c#
[TypeWrapper(typeof(long))]
readonly partial struct Timestamp
{
    partial void OnCreate(ref long newValue)
    {
        Debug.Assert(newValue > 12433392000, "Historical dates not supported")
        newValue = WarpHelper.CorrectForTimeDilation(newValue);
    }
}
```


It's also possible to make the struct wrapper generic. This can be useful if you
want to use phantom types:

```c#
interface IRelative {};
interface IAbsolute {};

[TypeWrapper(typeof(string)]
readonly partial struct Path<T>
{
}

Path<IRelative> build = new("Build/");
```



