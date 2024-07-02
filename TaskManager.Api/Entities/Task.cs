namespace TaskManager.Api.Entities;

public class Task_m {
	public required string id { get; set; }
	public required string title { get; set; }
	public required string description { get; set; }
	public required string status { get; set; }
	public required string deadline { get; set; }
	public string? UserId { get; set; }
	public User? user { get; set; }
}
