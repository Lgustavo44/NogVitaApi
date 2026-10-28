namespace NogVita.Application.Common;

// As respostas pequenas que antes eram objetos anônimos nos controllers (new { id = ... }).
// Tipadas, elas aparecem no OpenAPI (D184), e o JSON continua o mesmo: { "id": ... } e { "message": ... }.
public sealed record IdResponse(Guid Id);

public sealed record MessageResponse(string Message);
