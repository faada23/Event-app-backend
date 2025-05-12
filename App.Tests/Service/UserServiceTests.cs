using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class UserServiceTests
{
    private readonly Mock<IRepository<User>> _mockUserRepository;
    private readonly Mock<IRepository<Event>> _mockEventRepository;
    private readonly Mock<IRepository<EventParticipant>> _mockEventParticipantRepository;
    private readonly Mock<IDefaultMapper> _mockMapper;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IRepository<User>>();
        _mockEventRepository = new Mock<IRepository<Event>>();
        _mockEventParticipantRepository = new Mock<IRepository<EventParticipant>>();
        _mockMapper = new Mock<IDefaultMapper>();

        _userService = new UserService(
            _mockUserRepository.Object,
            _mockEventRepository.Object,
            _mockEventParticipantRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnSuccessWithUserResponse()
    {
        var userId = Guid.NewGuid();
        var userBirthDate = new DateOnly(1990, 1, 1);
        var userRegDate = DateTimeOffset.UtcNow;
        var user = new User { Id = userId, Email = "test@example.com", FirstName = "Test", LastName = "User1", DateOfBirth = userBirthDate, SystemRegistrationDate = userRegDate };
        var userResponse = new GetUserResponse(userId, "Test", "User1", "test@example.com", userBirthDate, userRegDate);

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(user));

        _mockMapper.Setup(mapper => mapper.Map<User, GetUserResponse>(user))
            .Returns(userResponse);

        var result = await _userService.GetUserById(userId);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(userResponse);
        _mockUserRepository.Verify(repo => repo.GetFirstOrDefault(
            It.Is<Expression<Func<User, bool>>>(expr => expr.Compile().Invoke(new User { Id = userId })),
            null), Times.Once);
        _mockMapper.Verify(m => m.Map<User, GetUserResponse>(user), Times.Once);
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldReturnFailureRecordNotFound()
    {
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(null));

        var result = await _userService.GetUserById(userId);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.RecordNotFound);
        result.Message.Should().Contain($"User with ID {userId} not found.");
    }

    [Fact]
    public async Task GetUserById_WhenRepositoryFails_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Failure("Database error", ErrorType.DatabaseError));

        var result = await _userService.GetUserById(userId);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.DatabaseError);
        result.Message.Should().Be("Database error");
    }

    [Fact]
    public async Task UpdateUser_WhenUserExistsAndEmailNotTaken_ShouldReturnSuccessWithUpdatedUser()
    {
        var userId = Guid.NewGuid();
        var birthDate = new DateOnly(1990, 1, 1);
        var registrationDate = DateTimeOffset.UtcNow;
        var existingUser = new User { Id = userId, Email = "old@example.com", FirstName = "Old", LastName = "LastName", DateOfBirth = birthDate, SystemRegistrationDate = registrationDate };

        var request = new UpdateUserRequest("new@example.com", "New", "User", birthDate);

        var updatedUserResponse = new GetUserResponse(userId, "New", "User", "new@example.com", birthDate, registrationDate);

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.Is<Expression<Func<User, bool>>>(e => e.Compile().Invoke(new User { Id = userId })),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(existingUser));

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.Is<Expression<Func<User, bool>>>(e => e.Compile().Invoke(new User { Email = request.Email, Id = Guid.NewGuid() })),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(null));

        _mockMapper.Setup(m => m.Map(request, existingUser)).Callback((UpdateUserRequest req, User usr) =>
        {
            usr.Email = req.Email;
            usr.FirstName = req.FirstName;
            usr.LastName = req.LastName;
            usr.DateOfBirth = req.DateOfBirth;
        });
        _mockUserRepository.Setup(repo => repo.Update(existingUser));
        _mockUserRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(Result<int>.Success(1));
        _mockMapper.Setup(mapper => mapper.Map<User, GetUserResponse>(existingUser))
            .Returns(updatedUserResponse);

        var result = await _userService.UpdateUser(userId, request);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(updatedUserResponse);
        _mockUserRepository.Verify(repo => repo.Update(It.Is<User>(u => u.Email == request.Email && u.FirstName == request.FirstName)), Times.Once);
        _mockUserRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFound_ShouldReturnFailureRecordNotFound()
    {
        var userId = Guid.NewGuid();
        var birthDate = new DateOnly(1990, 1, 1);
        var request = new UpdateUserRequest("new@example.com", "New", "User", birthDate);

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(null));

        var result = await _userService.UpdateUser(userId, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.RecordNotFound);
    }

    [Fact]
    public async Task UpdateUser_WhenEmailIsAlreadyInUse_ShouldReturnFailureAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var birthDate = new DateOnly(1990, 1, 1);
        var existingUser = new User { Id = userId, Email = "old@example.com", FirstName = "Old", LastName = "LN", DateOfBirth = birthDate };
        var request = new UpdateUserRequest("taken@example.com", "F", "L", birthDate);

        var otherUserWithEmail = new User { Id = Guid.NewGuid(), Email = "taken@example.com" };

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.Is<Expression<Func<User, bool>>>(e => e.Compile().Invoke(new User { Id = userId })),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(existingUser));

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.Is<Expression<Func<User, bool>>>(e => e.Compile().Invoke(new User { Email = request.Email, Id = Guid.NewGuid() })),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(otherUserWithEmail));

        var result = await _userService.UpdateUser(userId, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.AlreadyExists);
        result.Message.Should().Contain($"Email '{request.Email}' is already in use");
    }

    [Fact]
    public async Task UpdateUser_WhenSaveChangesFails_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var birthDate = new DateOnly(1990, 1, 1);
        var existingUser = new User { Id = userId, Email = "old@example.com", FirstName = "Old", LastName = "L", DateOfBirth = birthDate };
        var request = new UpdateUserRequest("new@example.com", "N", "U", birthDate);


        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
               It.Is<Expression<Func<User, bool>>>(e => e.Compile().Invoke(new User { Id = userId })),
               It.IsAny<string?>()))
           .ReturnsAsync(Result<User?>.Success(existingUser));
        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
               It.Is<Expression<Func<User, bool>>>(e => e.Compile().Invoke(new User { Email = request.Email, Id = Guid.NewGuid() })),
               It.IsAny<string?>()))
           .ReturnsAsync(Result<User?>.Success(null));

        _mockUserRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(Result<int>.Failure("DB Save Error", ErrorType.DatabaseError));

        var result = await _userService.UpdateUser(userId, request);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.DatabaseError);
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldReturnSuccessTrue()
    {
        var userId = Guid.NewGuid();
        var userToDelete = new User { Id = userId };

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(userToDelete));
        _mockUserRepository.Setup(repo => repo.Delete(userToDelete));
        _mockUserRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(Result<int>.Success(1));

        var result = await _userService.DeleteUser(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _mockUserRepository.Verify(repo => repo.Delete(userToDelete), Times.Once);
        _mockUserRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_ShouldReturnFailureRecordNotFound()
    {
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(null));

        var result = await _userService.DeleteUser(userId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.RecordNotFound);
    }

    [Fact]
    public async Task ParticipateInEvent_WhenValidAndNotParticipating_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var registrationDate = DateTimeOffset.UtcNow; 
        var user = new User { Id = userId };
        var evt = new Event { Id = eventId, Name = "Test Event" };

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(user));
        _mockEventRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<Event?>.Success(evt));
        _mockEventParticipantRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<EventParticipant, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<EventParticipant?>.Success(null));

        _mockEventParticipantRepository.Setup(repo => repo.Insert(It.IsAny<EventParticipant>()));
        _mockEventParticipantRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(Result<int>.Success(1));

        _mockMapper.Setup(m => m.Map<EventParticipant, UserEventParticipationResponse>(It.IsAny<EventParticipant>()))
            .Returns<EventParticipant>(ep => new UserEventParticipationResponse(ep.UserId, ep.EventId, ep.EventRegistrationDate));


        var result = await _userService.ParticipateInEvent(userId, eventId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value?.UserId.Should().Be(userId);
        result.Value?.EventId.Should().Be(eventId);
        _mockEventParticipantRepository.Verify(repo => repo.Insert(It.Is<EventParticipant>(ep => ep.UserId == userId && ep.EventId == eventId)), Times.Once);
        _mockEventParticipantRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ParticipateInEvent_WhenUserNotFound_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(null));

        var result = await _userService.ParticipateInEvent(userId, eventId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.RecordNotFound);
        result.Message.Should().Contain($"User with ID {userId} not found");
    }

    [Fact]
    public async Task ParticipateInEvent_WhenEventNotFound_ShouldReturnFailure()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var user = new User { Id = userId };

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(user));
        _mockEventRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<Event?>.Success(null));

        var result = await _userService.ParticipateInEvent(userId, eventId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.RecordNotFound);
        result.Message.Should().Contain($"Event with ID {eventId} not found");
    }


    [Fact]
    public async Task ParticipateInEvent_WhenAlreadyParticipating_ShouldReturnFailureAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var user = new User { Id = userId };
        var evt = new Event { Id = eventId };
        var existingParticipation = new EventParticipant { UserId = userId, EventId = eventId };

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(user));
        _mockEventRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<Event, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<Event?>.Success(evt));
        _mockEventParticipantRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<EventParticipant, bool>>>(), It.IsAny<string?>()))
            .ReturnsAsync(Result<EventParticipant?>.Success(existingParticipation));

        var result = await _userService.ParticipateInEvent(userId, eventId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.AlreadyExists);
        result.Message.Should().Be("User is already participating in this event.");
    }

    [Fact]
    public async Task GetAllUsers_WhenUsersExist_ShouldReturnPagedSuccess()
    {
        var pagParams = new PaginationParameters { Page = 1, PageSize = 10 };
        var user1Birth = new DateOnly(1990,1,1);
        var user1Reg = DateTimeOffset.UtcNow.AddDays(-2);
        var user2Birth = new DateOnly(1992,2,2);
        var user2Reg = DateTimeOffset.UtcNow.AddDays(-1);

        var users = new List<User> {
            new User { Id = Guid.NewGuid(), FirstName = "A", LastName = "X", Email="a@x.com", DateOfBirth=user1Birth, SystemRegistrationDate=user1Reg},
            new User { Id = Guid.NewGuid(), FirstName = "B", LastName = "Y", Email="b@y.com", DateOfBirth=user2Birth, SystemRegistrationDate=user2Reg}
        };
        var pagedUsers = PagedList<User>.ToPagedList(users, pagParams.Page, pagParams.PageSize, users.Count);

        var userResponses = users.Select(u => new GetUserResponse(u.Id, u.FirstName, u.LastName, u.Email, u.DateOfBirth, u.SystemRegistrationDate)).ToList();

        var totalPages = (int)Math.Ceiling((double)users.Count / pagParams.PageSize);
        var pagedResponseDto = new PagedResponse<GetUserResponse>(userResponses, pagParams.Page, pagParams.PageSize, users.Count, totalPages);


        _mockUserRepository.Setup(repo => repo.GetAll(
                null,
                pagParams,
                It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
                null))
            .ReturnsAsync(Result<PagedList<User>>.Success(pagedUsers));

        _mockMapper.Setup(m => m.Map<PagedList<User>, PagedResponse<GetUserResponse>>(pagedUsers))
             .Returns(pagedResponseDto);

        var result = await _userService.GetAllUsers(pagParams);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(pagedResponseDto);
        result.Value?.Data.Should().HaveCount(users.Count);
        result.Value?.TotalItems.Should().Be(users.Count);

        _mockUserRepository.Verify(repo => repo.GetAll(
            null,
            pagParams,
            It.IsAny<Func<IQueryable<User>, IOrderedQueryable<User>>>(),
            null), Times.Once);
    }

    [Fact]
    public async Task CancelEventParticipation_WhenParticipating_ShouldReturnSuccessTrue()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var participation = new EventParticipant { UserId = userId, EventId = eventId };

        _mockEventParticipantRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<EventParticipant, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<EventParticipant?>.Success(participation));
        _mockEventParticipantRepository.Setup(repo => repo.Delete(participation));
        _mockEventParticipantRepository.Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(Result<int>.Success(1));

        var result = await _userService.CancelEventParticipation(userId, eventId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _mockEventParticipantRepository.Verify(repo => repo.Delete(participation), Times.Once);
        _mockEventParticipantRepository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelEventParticipation_WhenNotParticipating_ShouldReturnFailureRecordNotFound()
    {
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        _mockEventParticipantRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<EventParticipant, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<EventParticipant?>.Success(null));


        var result = await _userService.CancelEventParticipation(userId, eventId);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.RecordNotFound);
        result.Message.Should().Be("User is not participating in this event.");
    }

    [Fact]
    public async Task GetUserParticipatedEvents_WhenUserExistsAndHasParticipations_ShouldReturnPagedSuccess()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var pagParams = new PaginationParameters { Page = 1, PageSize = 5 };
        var eventTime1 = DateTimeOffset.UtcNow.AddHours(2);
        var eventTime2 = DateTimeOffset.UtcNow.AddHours(4);

        var participations = new List<EventParticipant>
        {
            new EventParticipant { UserId = userId, EventId = Guid.NewGuid(), Event = new Event { Name = "Event 1", DateTimeOfEvent = eventTime1, Location = "Online" }, EventRegistrationDate = DateTimeOffset.UtcNow.AddDays(-1) },
            new EventParticipant { UserId = userId, EventId = Guid.NewGuid(), Event = new Event { Name = "Event 2", DateTimeOfEvent = eventTime2, Location = "Office" }, EventRegistrationDate = DateTimeOffset.UtcNow }
        };
        var pagedParticipations = PagedList<EventParticipant>.ToPagedList(participations, pagParams.Page, pagParams.PageSize, participations.Count);

        var eventResponses = participations
            .Select(p => new UserParticipatedEventResponse(p.EventId, p.Event.Name, p.Event.DateTimeOfEvent, p.Event.Location))
            .ToList();

        var totalPagesParticipated = (int)Math.Ceiling((double)participations.Count / pagParams.PageSize);
        var pagedResponseDto = new PagedResponse<UserParticipatedEventResponse>(eventResponses, pagParams.Page, pagParams.PageSize, participations.Count, totalPagesParticipated);


        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.Is<Expression<Func<User, bool>>>(u => u.Compile().Invoke(new User { Id = userId })),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(user));

        _mockEventParticipantRepository.Setup(repo => repo.GetAll(
                It.Is<Expression<Func<EventParticipant, bool>>>(ep => ep.Compile().Invoke(new EventParticipant { UserId = userId })),
                pagParams,
                It.IsAny<Func<IQueryable<EventParticipant>, IOrderedQueryable<EventParticipant>>>(),
                "Event"))
            .ReturnsAsync(Result<PagedList<EventParticipant>>.Success(pagedParticipations));

        _mockMapper.Setup(m => m.Map<PagedList<EventParticipant>, PagedResponse<UserParticipatedEventResponse>>(pagedParticipations))
            .Returns(pagedResponseDto);

        var result = await _userService.GetUserParticipatedEvents(userId, pagParams);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(pagedResponseDto);
        result.Value?.Data.Should().HaveCount(participations.Count);
        result.Value?.TotalItems.Should().Be(participations.Count);


        _mockEventParticipantRepository.Verify(repo => repo.GetAll(
                It.Is<Expression<Func<EventParticipant, bool>>>(ex => ex.Compile().Invoke(new EventParticipant { UserId = userId })),
                pagParams,
                It.IsAny<Func<IQueryable<EventParticipant>, IOrderedQueryable<EventParticipant>>>(),
                "Event"), Times.Once);
    }

    [Fact]
    public async Task GetUserParticipatedEvents_WhenUserNotFound_ShouldReturnFailureRecordNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var pagParams = new PaginationParameters();

        _mockUserRepository.Setup(repo => repo.GetFirstOrDefault(
                It.IsAny<Expression<Func<User, bool>>>(),
                It.IsAny<string?>()))
            .ReturnsAsync(Result<User?>.Success(null));

        // Act
        var result = await _userService.GetUserParticipatedEvents(userId, pagParams);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.RecordNotFound);
        result.Message.Should().Contain($"User with ID {userId} not found");
    }
}