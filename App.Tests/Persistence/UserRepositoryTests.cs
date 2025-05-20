using Xunit;
using Moq; 
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace App.Tests.Persistence
{
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

            _context.Users = _context.Set<User>(); 
            _context.Events = _context.Set<Event>();
            _context.Categories = _context.Set<Category>();
            _context.Images = _context.Set<Image>();
            _context.Roles = _context.Set<Role>();
            _context.RefreshTokens = _context.Set<RefreshToken>();
            _context.EventParticipants = _context.Set<EventParticipant>();


            _userRepository = new Repository<User>(_context);
        }

        [Fact]
        public async Task GetFirstOrDefault_WhenUserExists_ShouldReturnUser()
        {
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Email = "test1@example.com", FirstName = "Test", LastName = "User1", DateOfBirth = new DateOnly(1990, 1, 1), PasswordHash = "hash", SystemRegistrationDate = DateTimeOffset.UtcNow };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(); 

            var resultUser = await _userRepository.GetFirstOrDefault(u => u.Email == "test1@example.com");

            resultUser.Should().NotBeNull();
            resultUser?.Id.Should().Be(userId);
            resultUser?.Email.Should().Be("test1@example.com");
        }

        [Fact]
        public async Task GetFirstOrDefault_WhenUserExistsWithInclude_ShouldReturnUserAndIncludedProperty()
        {
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();
            var role = new Role { Id = roleId, Name = "TestRole" };
            var user = new User
            {
                Id = userId, Email = "include@example.com", FirstName = "Inc", LastName = "User", DateOfBirth = new DateOnly(1990,1,1), PasswordHash="hash",
                Roles = new List<Role> { role } 
            };
            _context.Roles.Add(role); 
            _context.Users.Add(user); 
            await _context.SaveChangesAsync();

            var resultUser = await _userRepository.GetFirstOrDefault(u => u.Id == userId, includeProperties: "Roles");

            resultUser.Should().NotBeNull();
            resultUser?.Id.Should().Be(userId);
            resultUser?.Roles.Should().NotBeNullOrEmpty();
            resultUser?.Roles.First().Name.Should().Be("TestRole");
        }


        [Fact]
        public async Task GetFirstOrDefault_WhenUserDoesNotExist_ShouldReturnNull()
        {
            var resultUser = await _userRepository.GetFirstOrDefault(u => u.Email == "nonexistent@example.com");

            resultUser.Should().BeNull();
        }

        [Fact]
        public async Task Insert_And_SaveChangesAsync_WhenUserIsValid_ShouldAddUserToDatabaseAndReturnChangesCount()
        {
            var userId = Guid.NewGuid();
            var newUser = new User { Id = userId, Email = "newuser@example.com", FirstName = "New", LastName = "User", DateOfBirth = new DateOnly(1995, 5, 5), PasswordHash = "newhash", SystemRegistrationDate = DateTimeOffset.UtcNow };

            _userRepository.Insert(newUser); 
            int changesCount = await _userRepository.SaveChangesAsync(); 

            changesCount.Should().BeGreaterThan(0); 

            var userInDb = await _context.Users.FindAsync(userId); 
            userInDb.Should().NotBeNull();
            userInDb?.Email.Should().Be("newuser@example.com");
            userInDb?.FirstName.Should().Be("New");
        }

        [Fact]
        public async Task GetAll_WithPagination_ShouldReturnPagedListOfUsers()
        {
            _context.Users.Add(new User {
                Id = Guid.NewGuid(),
                Email = "u1@example.com",
                FirstName = "User1", 
                LastName = "A",
                PasswordHash = "hash1",
                DateOfBirth = new DateOnly(1990,1,1),
                SystemRegistrationDate = DateTimeOffset.UtcNow
            });
            _context.Users.Add(new User {
                Id = Guid.NewGuid(),
                Email = "u2@example.com",
                FirstName = "User2",
                LastName = "B",
                PasswordHash = "hash2",
                DateOfBirth = new DateOnly(1990,1,1), 
                SystemRegistrationDate = DateTimeOffset.UtcNow
            });
            _context.Users.Add(new User {
                Id = Guid.NewGuid(),
                Email = "u3@example.com",
                FirstName = "User3",
                LastName = "C",
                PasswordHash = "hash3", 
                DateOfBirth = new DateOnly(1990,1,1),
                SystemRegistrationDate = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync();

            var pagParams = new PaginationParameters { Page = 2, PageSize = 1 };
            Func<IQueryable<User>, IOrderedQueryable<User>> orderBy = q => q.OrderBy(u => u.LastName);

            var pagedResult = await _userRepository.GetAll(null, pagParams, orderBy, null);

            pagedResult.Should().NotBeNull();
            pagedResult.Should().HaveCount(1);
            pagedResult.First().LastName.Should().Be("B");
            pagedResult.CurrentPage.Should().Be(2);
            pagedResult.PageSize.Should().Be(1);
            pagedResult.TotalItems.Should().Be(3);
            pagedResult.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task Delete_And_SaveChangesAsync_WhenUserExists_ShouldRemoveUser()
        {
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                FirstName = "ToDelete",
                LastName = "User",
                Email = "todelete@example.com",
                DateOfBirth = new DateOnly(1980, 5, 15), 
                PasswordHash = "somehashedpassword",    
                SystemRegistrationDate = DateTimeOffset.UtcNow.AddDays(-10) 
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userToDelete = await _context.Users.FindAsync(userId); 
            userToDelete.Should().NotBeNull();

            _userRepository.Delete(userToDelete!); 
            var changes = await _userRepository.SaveChangesAsync();

            changes.Should().Be(1); 
            var deletedUserInDb = await _context.Users.FindAsync(userId);
            deletedUserInDb.Should().BeNull();
        }


        public void Dispose()
        {
            _context.Database.EnsureDeleted(); 
            _context.Dispose();
        }
    }
}