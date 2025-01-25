
# C# Type Wrapper Generator

How to install: Use the TypeWrapperSourceGenerator project to create the source generator dll. 
Add it together with Types/Feature.cs and Types/TypeWrapperAttribute.cs to your project.

Then you can use the following syntax to get a type-safe wrapper generated for your primitive types:

```
[TypeWrapper(typeof(string))]
readonly partial struct UserId
{
}
```

You can also automatically generator a JsonConverter in case you use NewtonSoft Json:

```
[TypeWrapper(typeof(string), Feature.NewtonSoftJsonConverter)]
readonly partial struct UserId
{
}
```


It's also possible to make the struct wrapper generic. This can be useful if you
want to use phantom types:

```
interface IRelative {};
interface IAbsolute {};

[TypeWrapper(typeof(string)]
readonly partial struct Path<T>
{
}

Path<IRelative> build = new("Build/");
```
