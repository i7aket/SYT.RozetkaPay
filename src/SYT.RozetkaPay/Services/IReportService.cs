using SYT.RozetkaPay.Models.Reports;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Contract for report generation operations. Implemented by <see cref="ReportService"/> and
/// intended as the injection/mocking seam for consumer code.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Get payments report
    /// </summary>
    /// <param name="request">Payments report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payments report response</returns>
    Task<PaymentsReportResponse> GetPaymentsReportAsync(PaymentsReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transactions report
    /// </summary>
    /// <param name="request">Transactions report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transactions report response</returns>
    Task<TransactionsReportResponse> GetTransactionsReportAsync(TransactionsReportRequest request, CancellationToken cancellationToken = default);
}
