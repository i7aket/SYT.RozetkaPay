using SYT.RozetkaPay.Configuration;
using SYT.RozetkaPay.Models.Reports;
using Microsoft.Extensions.Logging;

namespace SYT.RozetkaPay.Services;

/// <summary>
/// Service for generating reports
/// </summary>
public class ReportService : BaseService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReportService"/> class.
    /// </summary>
    /// <param name="configuration">SDK configuration.</param>
    /// <param name="httpClient">HTTP client.</param>
    /// <param name="logger">Optional logger.</param>
    public ReportService(RozetkaPayConfiguration configuration, HttpClient httpClient, ILogger? logger = null)
        : base(configuration, httpClient, logger)
    {
    }

    /// <summary>
    /// Get payments report
    /// POST /api/reports/v1/payments
    /// </summary>
    /// <param name="request">Payments report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payments report response</returns>
    public async Task<PaymentsReportResponse> GetPaymentsReportAsync(PaymentsReportRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<PaymentsReportRequest, PaymentsReportResponse>("/api/reports/v1/payments", request, cancellationToken);
    }

    /// <summary>
    /// Get transactions report
    /// POST /api/reports/v1/transactions
    /// </summary>
    /// <param name="request">Transactions report request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transactions report response</returns>
    public async Task<TransactionsReportResponse> GetTransactionsReportAsync(TransactionsReportRequest request, CancellationToken cancellationToken = default)
    {
        return await PostAsync<TransactionsReportRequest, TransactionsReportResponse>("/api/reports/v1/transactions", request, cancellationToken);
    }
} 
