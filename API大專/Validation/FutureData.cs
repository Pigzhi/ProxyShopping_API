namespace API大專.Validation;
using System;
using System.ComponentModel.DataAnnotations;

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
            return false;

        if (value is DateTime dateTime)
        {
            // 只比日期，不比時間（避免今天但時間早被擋）
            return dateTime.Date > DateTime.Today;
        }

        return false;
    }
}