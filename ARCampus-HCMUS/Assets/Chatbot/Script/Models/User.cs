using System;

namespace ChatApp.Models
{
    [Serializable]
    public class User
    {
        public string id;
        public string username;
        public string user_role;   
        public bool IsGuest => user_role == "guest";
        public bool IsStudent => user_role == "student";
        public bool IsStaff => user_role == "staff";
    }

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string password;
        public string full_name;
        public string student_id;
    }

    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }
}
