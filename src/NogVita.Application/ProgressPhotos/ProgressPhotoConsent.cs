namespace NogVita.Application.ProgressPhotos;

// D-V2-16: mudou o texto, a versão sobe, e cada paciente precisa aceitar de novo antes do próximo envio.
// ⚠️ Revise o texto antes de publicar e mantenha a política de privacidade (docs/legal) coerente com ele.
public static class ProgressPhotoConsent
{
    public const string CurrentVersion = "1";

    public const string Text =
        "Autorizo o NogVita a guardar as fotos de evolução corporal que eu enviar. " +
        "Elas são dados pessoais sensíveis de saúde (LGPD, art. 11) e serão usadas só para acompanhar a minha evolução. " +
        "Só eu e o nutricionista que me acompanha no momento podemos vê-las; quando o acompanhamento termina, ele perde o acesso. " +
        "As fotos ficam num serviço de armazenamento contratado pelo NogVita (Supabase), e a localização e os outros dados do arquivo são removidos no envio. " +
        "Posso apagar qualquer foto a qualquer momento. Se eu retirar esta autorização, todas as minhas fotos de evolução serão apagadas.";
}
