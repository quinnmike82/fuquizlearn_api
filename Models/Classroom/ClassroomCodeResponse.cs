namespace fuquizlearn_api.Models.Classroom
{
    public class ClassroomCodeResponse
    {
        public int Id { get; set; }
        public int ClassroomId { get; set; }
        public string Code { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Expires { get; set; }
        public DateTime? Revoked { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => Revoked == null && !IsExpired;
    }
}
