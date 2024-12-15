namespace MethodCracker.Exceptions;

public class OriginMethodNotFoundException() : Exception(
    "Origin method not found, the assembly may not processed by MethodCracker");