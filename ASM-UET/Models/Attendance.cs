using System;
using System.Collections.Generic;

namespace ASM_UET.Models;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int EnrollmentId { get; set; }

    public DateOnly Date { get; set; }

    public string Status { get; set; } = null!;

    public int MarkedByTeacherId { get; set; }

    public virtual Enrollment Enrollment { get; set; } = null!;

    public virtual User MarkedByTeacher { get; set; } = null!;
}
