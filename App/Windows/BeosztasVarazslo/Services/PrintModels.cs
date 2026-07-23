namespace BeosztasVarazslo.Services;

public record PrintRow(string Label, bool IsGroupHeader, List<string> DayTexts, string BalanceText);

public record PrintDocument(string Title, List<string> DayHeaderLabels, List<PrintRow> Rows);
