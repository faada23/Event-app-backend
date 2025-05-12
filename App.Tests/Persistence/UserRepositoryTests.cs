using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class UserRepositoryTests : IDisposable
{
    private readonly DatabaseContext _context;
    private readonly Repository<User> _userRepository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
            .Options;
        _context = new DatabaseContext(options);
        _userRepository = new Repository<User>(_context);
    }

    [Fact]
    public async Task GetFirstOrDefault_WhenUserExists_ShouldReturnSuccessWithUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test1@example.com", FirstName = "Test", LastName = "User1", DateOfBirth = new DateOnly(1990, 1, 1), PasswordHash = "hash", SystemRegistrationDate = DateTimeOffset.UtcNow };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userRepository.GetFirstOrDefault(u => u.Email == "test1@example.com");

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value?.Id.Should().Be(userId);
        result.Value?.Email.Should().Be("test1@example.com");
    }

    [Fact]
    public async Task GetFirstOrDefault_WhenUserExistsWithInclude_ShouldReturnSuccessWithUserAndIncludedProperty()
    {
        var userIdSimple = Guid.NewGuid();
        var userSimple = new User
        {
            Id = userIdSimple,
            Email = "simple@example.com",
            FirstName = "Simple",
            LastName = "SimpleLastName", 
            PasswordHash = "simplehash", 
            DateOfBirth = new DateOnly(2000, 1, 1) 
        };
        _context.Users.Add(userSimple);
        await _context.SaveChangesAsync();

        var resultNullInclude = await _userRepository.GetFirstOrDefault(u => u.Id == userIdSimple, includeProperties: null);

        var resultEmptyInclude = await _userRepository.GetFirstOrDefault(u => u.Id == userIdSimple, includeProperties: "");

        resultNullInclude.IsSuccess.Should().BeTrue();
        resultNullInclude.Value.Should().NotBeNull();
        resultNullInclude.Value?.Email.Should().Be("simple@example.com");

        resultEmptyInclude.IsSuccess.Should().BeTrue();
        resultEmptyInclude.Value.Should().NotBeNull();
        resultEmptyInclude.Value?.Email.Should().Be("simple@example.com");
    }


    [Fact]
    public async Task GetFirstOrDefault_WhenUserDoesNotExist_ShouldReturnSuccessWithNullValue()
    {

        var result = await _userRepository.GetFirstOrDefault(u => u.Email == "nonexistent@example.com");

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();      
    }

    [Fact]
    public async Task Insert_And_SaveChangesAsync_WhenUserIsValid_ShouldAddUserToDatabaseAndReturnSuccess()
    {
        var userId = Guid.NewGuid();
        var newUser = new User { Id = userId, Email = "newuser@example.com", FirstName = "New", LastName = "User", DateOfBirth = new DateOnly(1995, 5, 5), PasswordHash = "newhash", SystemRegistrationDate = DateTimeOffset.UtcNow };


        _userRepository.Insert(newUser);
        var saveResult = await _userRepository.SaveChangesAsync();

        // Assert
        saveResult.Should().NotBeNull();
        saveResult.IsSuccess.Should().BeTrue();
        saveResult.Value.Should().BeGreaterThan(0); 

        var userInDb = await _context.Users.FindAsync(userId); 
        userInDb.Should().NotBeNull();
        userInDb?.Email.Should().Be("newuser@example.com");
        userInDb?.FirstName.Should().Be("New");
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDbUpdateExceptionOccurs_ShouldReturnFailureWithDatabaseError()
    {

        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString() + "_error_test_dbset") // Уникальное имя
            .Options;

        var mockContext = new Mock<DatabaseContext>(options);

        var mockUserDbSet = new Mock<DbSet<User>>();
        mockContext.Setup(db => db.Set<User>()).Returns(mockUserDbSet.Object);

        mockContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DbUpdateException("Simulated DB update error", new Exception("Inner exception details")));

        var repositoryWithMockedContext = new Repository<User>(mockContext.Object);
        var newUser = new User {
            Id = Guid.NewGuid(),
            Email = "error@example.com",
            FirstName = "Error",
            LastName = "User", 
            PasswordHash = "errorhash", 
            DateOfBirth = new DateOnly(1999,1,1) 
        };

        repositoryWithMockedContext.Insert(newUser);
        var result = await repositoryWithMockedContext.SaveChangesAsync();

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.DatabaseError);
        result.Message.Should().Be("Simulated DB update error");
        mockContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once()); 
    }


    public void Dispose()
    {
        _context.Database.EnsureDeleted(); 
        _context.Dispose();
    }
}