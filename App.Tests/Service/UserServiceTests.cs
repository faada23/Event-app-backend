using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class UserServiceTests
{
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IRepository<Event>> _eventRepositoryMock;
    private readonly Mock<IRepository<EventParticipant>> _eventParticipantRepositoryMock;
    private readonly Mock<IDefaultMapper> _mapperMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IRepository<User>>();
        _eventRepositoryMock = new Mock<IRepository<Event>>();
        _eventParticipantRepositoryMock = new Mock<IRepository<EventParticipant>>();
        _mapperMock = new Mock<IDefaultMapper>();

        _userService = new UserService(
            _userRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _eventParticipantRepositoryMock.Object,
            _mapperMock.Object
        );
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnUserResponse()
    {
        var userId = Guid.NewGuid();
        var userEntity = new User { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User" };
        var expectedDto = new GetUserResponse(userId, "Test", "User", "test@example.com", DateOnly.FromDateTime(DateTime.Now), DateTimeOffset.UtcNow);

        _userRepositoryMock
            .Setup(repo => repo.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(userEntity);
        _mapperMock
            .Setup(m => m.Map<User, GetUserResponse>(userEntity))
            .Returns(expectedDto);

        var result = await _userService.GetUserById(userId);

        result.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock
            .Setup(repo => repo.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync((User?)null);

        var action = async () => await _userService.GetUserById(userId);
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUser_WhenValid_ShouldUpdateAndReturnResponse()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest("New", "Name", "new@example.com", new DateOnly(1990, 1, 1));
        var existingUser = new User { Id = userId, Email = "old@example.com", FirstName = "Old", LastName = "User", SystemRegistrationDate = DateTimeOffset.UtcNow };
        var mappedResponse = new GetUserResponse(userId, request.FirstName, request.LastName, request.Email, request.DateOfBirth, existingUser.SystemRegistrationDate);

        _userRepositoryMock.SetupSequence(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(existingUser)  
            .ReturnsAsync((User?)null);   

        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map(request, existingUser))
                .Callback<UpdateUserRequest, User>((src, dest) => {
                    dest.FirstName = src.FirstName;
                    dest.LastName = src.LastName;
                    dest.Email = src.Email;
                    dest.DateOfBirth = src.DateOfBirth;
                });

        _mapperMock.Setup(m => m.Map<User, GetUserResponse>(existingUser))
                .Returns(mappedResponse);

        var result = await _userService.UpdateUser(userId, request);

        result.Should().NotBeNull("UpdateUser should return a response.");
        result.Should().BeEquivalentTo(mappedResponse);
        _userRepositoryMock.Verify(r => r.Update(existingUser), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        _userRepositoryMock.Verify(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null), Times.Exactly(2)); // Проверяем оба вызова
    }

    private bool IsCheckingEmail(Expression<Func<User, bool>> expr, string email, Guid excludeId)
    {
        var body = expr.Body.ToString();
        return body.Contains($"u.Email == \"{email}\"") && body.Contains($"u.Id != value(") && body.Contains(excludeId.ToString());
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest("New", "Name", "new@example.com", new DateOnly(1990, 1, 1));
        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User?)null);

        var action = async () => await _userService.UpdateUser(userId, request);
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUser_WhenEmailAlreadyExistsForAnotherUser_ShouldThrowAlreadyExistsException()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest("New", "Name", "taken@example.com", new DateOnly(1990, 1, 1));
        var existingUser = new User { Id = userId, Email = "old@example.com" };
        var otherUserWithEmail = new User { Id = Guid.NewGuid(), Email = "taken@example.com" };

        _userRepositoryMock.SetupSequence(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(otherUserWithEmail);

        var action = async () => await _userService.UpdateUser(userId, request);
        await action.Should().ThrowAsync<AlreadyExistsException>();
    }

    [Fact]
    public async Task UpdateUser_WhenSaveChangesFails_ShouldThrowDbUpdateExceptionFromService() 
    {
        var userId = Guid.NewGuid();
        var request = new UpdateUserRequest("New", "Name", "new@example.com", new DateOnly(1990, 1, 1));
        var existingUser = new User { Id = userId, Email = "old@example.com", SystemRegistrationDate = DateTimeOffset.UtcNow };
        var dbUpdateEx = new DbUpdateException("DB error on update", new Exception("Inner update exception"));

        _userRepositoryMock.SetupSequence(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null))
            .ReturnsAsync(existingUser)       
            .ReturnsAsync((User?)null);     

        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateEx); 

        var action = async () => await _userService.UpdateUser(userId, request);

        var exceptionAssertions = await action.Should().ThrowAsync<DbUpdateException>();
        exceptionAssertions.WithMessage("DB error on update");
        exceptionAssertions.WithInnerException<Exception>().WithMessage("Inner update exception");
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var userEntity = new User { Id = userId };
        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(userEntity);
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _userService.DeleteUser(userId);

        result.Should().BeTrue();
        _userRepositoryMock.Verify(r => r.Delete(userEntity), Times.Once);
        _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User?)null);

        var action = async () => await _userService.DeleteUser(userId);
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteUser_WhenSaveChangesFails_ShouldThrowDbUpdateException() 
    {
        var userId = Guid.NewGuid();
        var userToDelete = new User { Id = userId };
        var dbUpdateEx = new DbUpdateException("DB error on delete", new Exception("Inner delete exception"));
        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(userToDelete);
        _userRepositoryMock.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateEx);

        var action = async () => await _userService.DeleteUser(userId);

        var exceptionAssertions = await action.Should().ThrowAsync<DbUpdateException>();
        exceptionAssertions.WithInnerException<Exception>();
    }

    [Fact]
    public async Task ParticipateInEvent_WhenValid_ShouldAddParticipationAndReturnResponse()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var user = new User { Id = userId };
        var ev = new Event { Id = eventId, MaxParticipants = 10, EventParticipants = new List<EventParticipant>() };
        var expectedResponse = new UserEventParticipationResponse(eventId, userId, DateTimeOffset.UtcNow);

        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<Event, bool>>>(), "EventParticipants")).ReturnsAsync(ev);
        _eventParticipantRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<EventParticipant, bool>>>(), null)).ReturnsAsync((EventParticipant?)null);
        _eventParticipantRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<EventParticipant, UserEventParticipationResponse>(It.IsAny<EventParticipant>()))
                    .Returns((EventParticipant ep) => new UserEventParticipationResponse(ep.EventId, ep.UserId, ep.EventRegistrationDate));


        var result = await _userService.ParticipateInEvent(userId, eventId);

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedResponse, options =>
        options.Using<DateTimeOffset>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1)))
                .WhenTypeIs<DateTimeOffset>());

        _eventParticipantRepositoryMock.Verify(r => r.Insert(It.Is<EventParticipant>(ep => ep.UserId == userId && ep.EventId == eventId)), Times.Once);
        _eventParticipantRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ParticipateInEvent_WhenEventIsFull_ShouldThrowBadRequestException()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var user = new User { Id = userId };
        var existingParticipant = new EventParticipant { EventId = eventId, UserId = Guid.NewGuid() };
        var ev = new Event { Id = eventId, MaxParticipants = 1, EventParticipants = new List<EventParticipant> { existingParticipant } };

        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
        _eventRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<Event, bool>>>(), "EventParticipants")).ReturnsAsync(ev);
        _eventParticipantRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<EventParticipant, bool>>>(), null)).ReturnsAsync((EventParticipant?)null); // Новый участник еще не зарегистрирован

        var action = async () => await _userService.ParticipateInEvent(userId, eventId);

        await action.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnPagedResponseOfUsers()
    {
        var users = new List<User> { new User { Id = Guid.NewGuid(), FirstName = "A" } };
        var pagedListUsers = PagedList<User>.ToPagedList(users, 1, 1, 1);
        var userDtos = users.Select(u => new GetUserResponse(u.Id, u.FirstName, u.LastName, u.Email, u.DateOfBirth, u.SystemRegistrationDate)).ToList();
        var expectedResponse = new PagedResponse<GetUserResponse>(userDtos, 1, 1, 1, 1);

        _userRepositoryMock
            .Setup(repo => repo.GetAll(null, It.IsAny<PaginationParameters>(), It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(), null))
            .ReturnsAsync(pagedListUsers);
        _mapperMock
            .Setup(m => m.Map<PagedList<User>, PagedResponse<GetUserResponse>>(pagedListUsers))
            .Returns(expectedResponse);

        var pagParams = new PaginationParameters { Page = 1, PageSize = 1 };
        var result = await _userService.GetAllUsers(pagParams);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task CancelEventParticipation_WhenParticipationExists_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var participation = new EventParticipant { UserId = userId, EventId = eventId };

        _eventParticipantRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<EventParticipant, bool>>>(), null)).ReturnsAsync(participation);
        _eventParticipantRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _userService.CancelEventParticipation(userId, eventId);

        result.Should().BeTrue();
        _eventParticipantRepositoryMock.Verify(r => r.Delete(participation), Times.Once);
        _eventParticipantRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelEventParticipation_WhenParticipationNotFound_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _eventParticipantRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<EventParticipant, bool>>>(), null)).ReturnsAsync((EventParticipant?)null);

        var action = async () => await _userService.CancelEventParticipation(userId, eventId);
        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetUserParticipatedEvents_WhenUserExists_ShouldReturnPagedEvents()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var event1 = new Event { Id = Guid.NewGuid(), Name = "Event 1", Category = new Category { Name = "Tech" }, Location = "Online" };
        var participations = new List<EventParticipant> { new EventParticipant { UserId = userId, EventId = event1.Id, Event = event1, EventRegistrationDate = DateTimeOffset.UtcNow } };
        var pagedParticipations = PagedList<EventParticipant>.ToPagedList(participations, 1, 1, 1);
        var mappedResponses = participations.Select(p => new UserParticipatedEventResponse(p.Event.Id, p.Event.Name, p.EventRegistrationDate, p.Event.Location)).ToList();
        var expectedResponse = new PagedResponse<UserParticipatedEventResponse>(mappedResponses, 1, 1, 1, 1);

        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync(user);
        _eventParticipantRepositoryMock
            .Setup(r => r.GetAll(
                It.IsAny<Expression<Func<EventParticipant, bool>>>(), 
                It.IsAny<PaginationParameters>(),                     
                It.IsAny<Func<IQueryable<EventParticipant>, IOrderedQueryable<EventParticipant>>>(), 
                "Event"                                               
            ))
            .ReturnsAsync(pagedParticipations);
        _mapperMock.Setup(m => m.Map<PagedList<EventParticipant>, PagedResponse<UserParticipatedEventResponse>>(pagedParticipations))
                    .Returns(expectedResponse);

        var pagParams = new PaginationParameters();
        var result = await _userService.GetUserParticipatedEvents(userId, pagParams);

        result.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetUserParticipatedEvents_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetFirstOrDefault(It.IsAny<Expression<Func<User, bool>>>(), null)).ReturnsAsync((User?)null);

        var action = async () => await _userService.GetUserParticipatedEvents(userId, new PaginationParameters());
        await action.Should().ThrowAsync<NotFoundException>();
    }
}
