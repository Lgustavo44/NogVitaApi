namespace NogVita.Application.Common;

// Conflito com um dado que já existe ou que outra pessoa acabou de mudar (D168): vira 409.
// A mensagem já vem pronta para o usuário.
public sealed class ConflictException(string message, Exception? innerException = null)
    : Exception(message, innerException);
