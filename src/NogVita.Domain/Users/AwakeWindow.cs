namespace NogVita.Domain.Users;

// O período em que o paciente está acordado, entre a hora de acordar e a de dormir.
public static class AwakeWindow
{
    // null quando não dá para saber: rotina não informada ou refeição sem horário.
    // Os limites contam como acordado: uma refeição às 06:00 para quem acorda às 06:00 não está fora.
    public static bool? IsOutside(TimeOnly? time, TimeOnly? wakeUpTime, TimeOnly? bedTime)
    {
        if (time is not { } t || wakeUpTime is not { } wake || bedTime is not { } bed)
        {
            return null;
        }

        // Janela normal (acorda 06:00, dorme 22:30): fora é antes de acordar ou depois de dormir.
        if (wake < bed)
        {
            return t < wake || t > bed;
        }

        // Janela que atravessa a meia-noite (acorda 14:00, dorme 06:00): fora é entre dormir e acordar.
        return t > bed && t < wake;
    }
}
