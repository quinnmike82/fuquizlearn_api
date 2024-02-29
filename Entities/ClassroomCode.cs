namespace fuquizlearn_api.Entities
{
    public class ClassroomCode
    {
        public int Id { get; set; }
        public Classroom Classroom { get; set; } 
        public string Code { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Expires { get; set; }
        public DateTime? Revoked { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => Revoked == null && !IsExpired;
    }
}
