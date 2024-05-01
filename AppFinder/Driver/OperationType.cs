using System.ComponentModel;

namespace AppFinder.Driver
{
    public enum OperationType
    {
        [Description("venda")]
        Sell,
        [Description("aluguel")]
        Rent,
        [Description("imoveis-lancamento")]
        LaunchPlan
    }
}
