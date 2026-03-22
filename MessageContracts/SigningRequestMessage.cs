namespace MessageContracts
{
	public record SigningRequestMessage(
		int DocumentId,
		int UserId,
		DateTime RequestedAt);
}
