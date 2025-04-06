using System;
using NUnit.Framework;
using Types;
using UnityEngine;

[TestFixture]
public class TestWrapperTests
{
    [TestCase]
    public void It_serializes_and_deserializes_a_value_to_json()
    {
        // Given  
        SerializableWrappedInt wrapped = new(123);
        SerializableWrappedString wrapped2 = new("test");

        // When
        string jsonInt = JsonUtility.ToJson(wrapped);
        SerializableWrappedInt deserializedInt = JsonUtility.FromJson<SerializableWrappedInt>(jsonInt);
        string jsonString = JsonUtility.ToJson(wrapped2);
        SerializableWrappedString deserializedString = JsonUtility.FromJson<SerializableWrappedString>(jsonString);

        // Then
        Assert.That(deserializedInt, Is.EqualTo(wrapped));
        Assert.That(deserializedString, Is.EqualTo(wrapped2));
    }
}