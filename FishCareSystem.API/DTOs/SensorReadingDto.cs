using System.ComponentModel.DataAnnotations;

namespace FishCareSystem.API.DTOs
{
    public class SensorReadingDto
    {
        public int Id { get; set; }
        public int TankId { get; set; }
        public string Type { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CreateSensorReadingDto
    {
        [Required]
        public int TankId { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public double Value { get; set; }

        [Required]
        public string Unit { get; set; }
    }

    // New DTOs for the enhanced functionality
    public class SensorStatisticsDto
    {
        public double Average { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public string Unit { get; set; }
        public int DataPoints { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class CurrentSensorReadingsDto
    {
        public int TankId { get; set; }
        public string TankName { get; set; }
        public List<CurrentSensorValueDto> Sensors { get; set; } = new List<CurrentSensorValueDto>();
    }

    public class CurrentSensorValueDto
    {
        public string Type { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Status { get; set; } // "Normal", "Warning", "Critical"
    }

    public class ChartDataDto
    {
        public SensorStatisticsDto Statistics { get; set; }
        public List<ChartPointDto> DataPoints { get; set; } = new List<ChartPointDto>();
        public string SensorType { get; set; }
        public string Unit { get; set; }
    }

    public class ChartPointDto
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string FormattedTime { get; set; } // "00:00", "04:00", etc.
    }

    public class SensorHistoryFilterDto
    {
        public int TankId { get; set; }
        public string SensorType { get; set; } // "Temperature", "pH", "WaterLevel", etc.
        public string TimePeriod { get; set; } // "1h", "6h", "10h", "24h", "7d", "30d"
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
