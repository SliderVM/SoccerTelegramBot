﻿using System.ComponentModel.DataAnnotations;

namespace SoccerTelegramBot.Entities
{
    public class Configuration
    {
        [Key]        
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
        [Required]
        public string Label { get; set; } = string.Empty;
    }
}
