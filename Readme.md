
# C# Type Wrapper Generator

How to install: Use the TypeWrapperSourceGenerator project to create the source generator dll. 
Add it together with Types/Feature.cs and Types/TypeWrapperAttribute.cs to your project.

Then you can use the following syntax to get a type-safe wrapper generated for your primitive types:

```
[TypeWrapper(typeof(string))]
public readonly partial struct UserId
{
}
```

You can also automatically generator a JsonConverter in case you use NewtonSoft Json:

```
[TypeWrapper(typeof(string), Feature.NewtonSoftJsonConverter)]
public readonly partial struct UserId
{
}
```
