// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "RCS1075:Avoid empty catch clause that catches System.Exception.",
    Justification = "This design pattern is used to silently handle several types of non-fatal errors", Scope = "module")]

[assembly: SuppressMessage("Readability", "RCS1123:Add parentheses when necessary.", Justification = "Parentheses aren't needed", Scope = "module")]
[assembly: SuppressMessage("Usage", "RCS1246:Use element access.", Justification = "Prefer to use .First()", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Legacy Name", Scope = "type", Target = "~T:MASICPeakFinder.clsPeakDetection")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Legacy Name", Scope = "type", Target = "~T:MASICPeakFinder.clsPeakInfo")]
