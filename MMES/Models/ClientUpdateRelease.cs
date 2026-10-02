using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MMES.Models
{
    [Table("tb_app_update_releases")]
    public class ClientUpdateRelease
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("version")]
        public string Version { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("original_file_name")]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("stored_file_name")]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("file_path")]
        public string FilePath { get; set; } = string.Empty;

        [Column("file_size")]
        public long FileSize { get; set; }

        [MaxLength(64)]
        [Column("file_hash_sha256")]
        public string FileHashSha256 { get; set; } = string.Empty;

        [Column("release_notes", TypeName = "text")]
        public string? ReleaseNotes { get; set; }

        [MaxLength(100)]
        [Column("uploaded_by")]
        public string? UploadedBy { get; set; }

        [Column("uploaded_at", TypeName = "datetime")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}
