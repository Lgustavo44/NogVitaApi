using NogVita.Domain.Anamneses;

namespace NogVita.Application.Anamneses;

// D-V2-27: o modelo base que o nutricionista copia e edita. Fica no código, no formato do corpo do POST:
// o front carrega, deixa editar e cria com o POST normal.
public static class QuestionnaireTemplate
{
    public static QuestionnaireRequest Base { get; } = new("Anamnese inicial",
    [
        new("Qual é o seu principal objetivo com o acompanhamento?", QuestionType.SingleChoice, true,
            ["Emagrecer", "Ganhar massa muscular", "Manter o peso", "Melhorar a saúde", "Melhorar o desempenho esportivo", "Outro"]),
        new("Conte um pouco mais sobre o seu objetivo.", QuestionType.Text, false, null),
        new("Você tem alergia ou intolerância a algum alimento?", QuestionType.MultipleChoice, false,
            ["Lactose", "Glúten", "Amendoim", "Frutos do mar", "Ovo", "Soja", "Outro"]),
        new("Se marcou \"Outro\", ou quer detalhar as alergias, escreva aqui.", QuestionType.Text, false, null),
        new("Você tem alguma doença diagnosticada (por exemplo, diabetes, hipertensão, doenças da tireoide)?", QuestionType.Text, false, null),
        new("Você toma algum medicamento ou suplemento? Quais?", QuestionType.Text, false, null),
        new("Você segue alguma restrição alimentar?", QuestionType.SingleChoice, false,
            ["Nenhuma", "Vegetariana", "Vegana", "Religiosa", "Outra"]),
        new("Como funciona o seu intestino?", QuestionType.SingleChoice, false,
            ["Todos os dias", "A cada 2 ou 3 dias", "Menos de 3 vezes por semana", "Diarreia frequente"]),
        new("Quantos copos de água você bebe por dia?", QuestionType.SingleChoice, false,
            ["Menos de 4", "De 4 a 8", "Mais de 8"]),
        new("Você bebe bebidas alcoólicas?", QuestionType.SingleChoice, false,
            ["Não", "Raramente", "Nos fins de semana", "Quase todos os dias"]),
        new("Você fuma?", QuestionType.YesNo, false, null),
        new("Você pratica atividade física? Qual, e quantas vezes por semana?", QuestionType.Text, false, null),
        new("Como é a sua rotina num dia comum (horários, trabalho, refeições fora de casa)?", QuestionType.Text, false, null),
        new("Quais alimentos você não come de jeito nenhum?", QuestionType.Text, false, null)
    ]);
}
