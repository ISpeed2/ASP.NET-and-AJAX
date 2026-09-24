namespace Shop.Domain;

public class Customer
{
    private Customer() { }

    public Customer(string email, string name)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email required", nameof(email));

        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        Name = name.Trim();
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsBlocked { get; private set; }

    public void Block() => IsBlocked = true;
    public void Unblock() => IsBlocked = false;
}
