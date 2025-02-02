namespace TaskManager.Api.Entities;

public class User {
	public required string id { get; set; }
	public required string fullname { get; set; }
	public required string email { get; set; }
	public required string password { get; set; }
}
