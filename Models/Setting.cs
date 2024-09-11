using System.ComponentModel.DataAnnotations;

namespace MiddleBooth.Models
{
    public class Setting
    {
        [Key]
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}