using UnityEngine;

namespace Types
{

    [CreateAssetMenu(fileName = "TestObject", menuName = "ScriptableObjects/TestObject")]
    public class UserData : ScriptableObject
    {
        [SerializeField] private string userId;
        public UserId UserId { get => new UserId(userId); set => userId = value.Value; }
        
    }
}