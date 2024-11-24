namespace MethodCracker.MonoCecil;

public class MethodCrackerProcessor(Stream processorConfig, Func<string, Stream> modulesResolver)
{
    private readonly Stream m_processorConfig = processorConfig;
    private readonly Func<string, Stream> m_modulesResolver = modulesResolver;
}