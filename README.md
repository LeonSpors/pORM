# pORM
[![Stable Version](https://img.shields.io/badge/Stable-Disabled-lightgrey?style=flat-square)]()
[![Nightly Version](https://img.shields.io/badge/Nightly-GitHub%20Packages-blueviolet?style=flat-square)](https://github.com/LeonSpors?tab=packages&repo_name=pORM)

[![Stable Workflow](https://github.com/LeonSpors/pORM/actions/workflows/publish-stable.yml/badge.svg)](https://github.com/LeonSpors/pORM/actions/workflows/publish-stable.yml)
[![Nightly Workflow](https://github.com/LeonSpors/pORM/actions/workflows/publish-nightly.yml/badge.svg)](https://github.com/LeonSpors/pORM/actions/workflows/publish-nightly.yml)




**pORM** is a performance-oriented, lightweight ORM mapper for .NET. It’s designed for developers who want a minimal and efficient way to map SQL query results to objects and perform CRUD operations without the overhead of a full-featured ORM.

## Features

- **Lightweight & Minimal:** Focused on performance and simplicity.
- **Easy Mapping:** Maps SQL query results to POCOs using reflection.
- **LINQ-to-SQL Translator:** Built-in expression translator for simple query expressions.
- **Asynchronous Support:** Async methods for executing SQL commands.
- **Extensible:** Designed to be extended and customized for your needs.

## Installation

You can install **pORM** via NuGet:

```powershell
Install-Package pORM
```

Or with the .NET CLI:

```bash
dotnet add package pORM
```

*Note: pORM is currently under heavy development, and breaking changes may be introduced in future releases.

## Usage

> **Disclaimer:**  
> The context-based mode is coming soon. For now, please refer to the examples below, which focus on using the global context and CRUD operations.

### Defining Your Entities
To work with pORM, start by defining your entity classes. Each entity must be decorated with a [Table] attribute so that pORM can map it to the correct database table. Use the [Key] attribute to mark the primary key property. For example:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Users")]
public class User
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}
```

### Basic CRUD Operations
pORM provides a global context interface that lets you work with tables for each entity. Once your entities are defined, you can perform CRUD (Create, Read, Update, Delete) operations. Here’s an example of how you might use the Global Context to perform these operations:
```csharp
public async Task ExampleCrudOperations(IGlobalContext globalContext)
{
    // Retrieve the table for the User entity.
    var userTable = globalContext.GetTable<User>();

    // Create a new user.
    var newUser = new User 
    { 
        Username = "john.doe", 
        Email = "john.doe@example.com" 
    };
    bool addResult = await userTable.AddAsync(newUser);
    
    // Update the user.
    newUser.Email = "john.new@example.com";
    bool updateResult = await userTable.UpdateAsync(newUser);
    
    // Check if the user exists.
    bool exists = await userTable.ExistsAsync(newUser);
    
    // Fetch a user that matches a condition.
    var user = await userTable.FirstOrDefaultAsync(u => u.Username == "john.doe");
    
    // Delete the user.
    bool removeResult = await userTable.RemoveAsync(newUser);
}
```

### Advanced Queries
pORM includes a built-in LINQ-to-SQL translator which lets you use LINQ expressions for advanced querying. This makes it straightforward to build queries using familiar syntax. For example:
```csharp
public async Task QueryUsersByEmailDomain(IGlobalContext globalContext)
{
    // Retrieve the table for the User entity.
    var userTable = globalContext.GetTable<User>();

    // Retrieve users whose email addresses end with a specific domain.
    IEnumerable<User> usersWithDomain = await userTable.WhereAsync(u => u.Email.EndsWith("@example.com"));

    // You can process or further query the results using standard LINQ methods.
    var usernames = usersWithDomain.Select(u => u.Username).ToList();
}
```

If you need more advanced querying capabilities, please refer to the documentation in the [/docs](/docs) folder.

## Nightly Packages

If you'd like to try out the very latest features and bug fixes before they make it into a stable release, nightly packages for pORM are available on GitHub Packages. These builds are updated on a nightly basis and include the bleeding-edge changes from the **develop** branch.

#### How to Use the Nightly Packages

1. **Authentication**  
   
   Make sure you are logged into GitHub. Your NuGet client may need to be configured to authenticate with GitHub Packages using your GitHub credentials or a Personal Access Token.

2. **Add the GitHub Packages Source**  
   
   Add the following package source to your `NuGet.config` file.
   ```xml
   <configuration>
     <packageSources>
       <add key="pORM Nightly" value="https://nuget.pkg.github.com/LeonSpors/index.json" />
     </packageSources>
   </configuration>
    ```

3. **Install the Nightly Package**
   
   Once you have added the package source, you can install the nightly version using the NuGet Package Manager or the Package Manager Console:
   ```powershell
    Install-Package pORM -Version <nightly-version>
   ```

   Replace <nightly-version> with the appropriate nightly version number. You can also search for pre-release versions in your package manager if you enable the "Include prerelease" option.

### Disclaimer

Nightly packages are pre-release builds and may contain experimental or unstable features. They are intended for testing and early feedback purposes and should be used with caution in production environments.

## Support

If you enjoy using pORM and want to support its development, please consider buying me a coffee!

[![Buy Me A Coffee](https://img.buymeacoffee.com/button-api/?text=Buy%20me%20a%20coffee&emoji=☕&slug=spors&button_colour=FFDD00&font_colour=000000&font_family=Poppins&outline_colour=000000&coffee_colour=ffffff)](https://www.buymeacoffee.com/spors)

## Contributing

Contributions are welcome! If you’d like to contribute, please:

1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Submit a pull request describing your changes.

Please open an issue first to discuss any major changes.

## License

pORM is licensed under the Apache License, Version 2.0.  
You may obtain a copy of the License in the [LICENSE](LICENSE) file.

© 2025 Leon Spors. All rights reserved.

## Contact

For questions or suggestions, please open an issue.