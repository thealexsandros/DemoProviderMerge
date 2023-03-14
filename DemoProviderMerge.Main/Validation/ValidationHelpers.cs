using System.Text;

namespace DemoProviderMerge.Main.Validation;

static public class ValidationHelpers
{
    static public void ValidateDateTime(this DateTime dateTime, string propertyName, StringBuilder errorBuilder)
    {
        if (dateTime == default)
        {
            errorBuilder.AppendLine($"{propertyName} should be specified.");
        }

        if (dateTime.Kind != DateTimeKind.Unspecified)
        {
            errorBuilder.AppendLine($"{propertyName} should have {nameof(dateTime.Kind)} = {DateTimeKind.Unspecified}.");
        }
    }

    static public void ValidateNullableDateTime(this DateTime? dateTime, string propertyName, StringBuilder errorBuilder)
    {
        if (dateTime != null)
        {
            ((DateTime)dateTime).ValidateDateTime(propertyName, errorBuilder);
        }
    }

    static public void ValidateDate(this DateTime date, string propertyName, StringBuilder errorBuilder)
    {
        date.ValidateDateTime(propertyName, errorBuilder);

        if (date.TimeOfDay != TimeSpan.Zero)
        {
            errorBuilder.AppendLine($"{propertyName} should not have time of day specified.");
        }
    }

    static public void ValidateNullableDate(this DateTime? date, string propertyName, StringBuilder errorBuilder)
    {
        if (date != null)
        {
            ((DateTime)date).ValidateDate(propertyName, errorBuilder);
        }
    }
}