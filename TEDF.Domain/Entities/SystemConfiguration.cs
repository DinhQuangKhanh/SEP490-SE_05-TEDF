using TEDF.Domain.Common.Primitives;
using TEDF.Domain.Enums.System;

namespace TEDF.Domain.Entities
{
    public class SystemConfiguration : Entity<int>
    {
        public string Key { get; private set; } = string.Empty;
        public string Value { get; private set; } = string.Empty;
        public ConfigDataType DataType { get; private set; }
        public string? Description { get; private set; }
        public string? Category { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public Guid? UpdatedBy { get; private set; }

        private SystemConfiguration() { }

        public static SystemConfiguration Create(
            int id,
            string key,
            string value,
            ConfigDataType dataType,
            string? description = null,
            string? category = null)
        {
            return new SystemConfiguration
            {
                Id = id,
                Key = key,
                Value = value,
                DataType = dataType,
                Description = description,
                Category = category,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public void UpdateValue(string value, Guid? updatedBy = null)
        {
            Value = value;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public T GetValue<T>()
        {
            return DataType switch
            {
                ConfigDataType.Int => (T)(object)int.Parse(Value),
                ConfigDataType.Bool => (T)(object)bool.Parse(Value),
                ConfigDataType.Json => System.Text.Json.JsonSerializer.Deserialize<T>(Value)!,
                _ => (T)(object)Value
            };
        }
    }
}
