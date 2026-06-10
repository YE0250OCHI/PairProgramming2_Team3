namespace TaxiManagementSystem.API.Model;

// JOB登録
public record RegisterJobRequest(string FromLoc, string ToLoc, string? TaxiId);

// タクシー再割当
public record ReassignTaxiRequest(string TaxiId);