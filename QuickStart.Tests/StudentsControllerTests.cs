using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QuickStart.Controllers.DTOs;
using QuickStart.Models;
using Xunit;

namespace QuickStart.Tests
{
    [Collection("Integration Tests")]
    public class StudentsControllerTests : IClassFixture<StudentsControllerTests.DbSetup>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        // This class provides setup before all class tests, then clean-up after
        [Collection("Integration Tests")]
        public class DbSetup : IDisposable
        {
            private RosterDbContext _dbContext;

            public DbSetup(WebApplicationFactory<Startup> factory)
            {
                // This fetches the same single lifetime instantiation used by Controller classes
                _dbContext = factory.Services.GetRequiredService<RosterDbContext>();

                // Seed in-memory database with some data needed for tests
                var school = new School
                {
                    Id = 1,
                    Name = "School of Hard Knocks",
                    City = "Life",
                    State = "Madness"
                };
                _dbContext.School.Add(school);
                var teacher = new Teacher
                {
                    Id = 1,
                    Name = "Mrs. Stricter",
                    School = school
                };
                _dbContext.Teacher.Add(teacher);
                var @class = new Class
                {
                    Id = 1,
                    Name = "Fifth Grade Class",
                    Teacher = teacher
                };
                _dbContext.Class.Add(@class);
                var student1 = new Student
                {
                    Id = 1,
                    Name = "Jim Bob",
                    Class = @class
                };
                _dbContext.Student.Add(student1);
                var student2 = new Student
                {
                    Id = 2,
                    Name = "Jane Doe",
                    Class = @class
                };
                _dbContext.Student.Add(student2);
                _dbContext.SaveChanges();
            }

            public void Dispose()
            {
                var students = _dbContext.Student.ToArray();
                _dbContext.Student.RemoveRange(students);
                var classes = _dbContext.Class.ToArray();
                _dbContext.Class.RemoveRange(classes);
                var teachers = _dbContext.Teacher.ToArray();
                _dbContext.Teacher.RemoveRange(teachers);
                var schools = _dbContext.School.ToArray();
                _dbContext.School.RemoveRange(schools);
                _dbContext.SaveChanges();
            }
        }

        public StudentsControllerTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetStudent_ReturnsSuccessAndStudent()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/students/1");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content);
            var responseStudent = JsonSerializer.Deserialize<StudentDTO>(
                await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            Assert.NotNull(responseStudent);
            Assert.Equal(1, responseStudent.Id);
            Assert.Equal("Jim Bob", responseStudent.Name);
            Assert.Equal(1, responseStudent.ClassId);
            Assert.Equal(1, responseStudent.TeacherId);
            Assert.Equal(1, responseStudent.SchoolId);
        }

        [Fact]
        public async Task GetStudent_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/students/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAllStudents_ReturnsSuccessAndStudents()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/students");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content);
            var responseStudents = JsonSerializer.Deserialize<IEnumerable<StudentDTO>>(
                await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            Assert.NotNull(responseStudents);
            Assert.Equal(2, responseStudents.Count());
            Assert.Contains(responseStudents, student => student.Id == 1);
            Assert.Contains(responseStudents, student => student.Id == 2);
        }

        [Fact]
        public async Task CreateStudent_ReturnsSuccessNewStudentAndLocationHeader()
        {
            // Arrange
            var client = _factory.CreateClient();
            var studentDto = new StudentDTO
            {
                Id = 3,
                Name = "John Doe",
                ClassId = 1
            };
            var content = new StringContent(JsonSerializer.Serialize(studentDto, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            try
            {
                // Act
                var response = await client.PostAsync("/students", content);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.NotNull(response.Content);
                var responseStudent = JsonSerializer.Deserialize<StudentDTO>(
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
                Assert.NotNull(responseStudent);
                Assert.Equal(studentDto.Id, responseStudent.Id);
                Assert.Equal(studentDto.Name, responseStudent.Name);
                Assert.Equal(studentDto.ClassId, responseStudent.ClassId);
                Assert.Equal(1, responseStudent.TeacherId);
                Assert.Equal(1, responseStudent.SchoolId);
                Assert.Equal(new Uri(client.BaseAddress, "/students/3"), response.Headers.Location);
            }
            finally
            {
                // Clean-up (so it doesn't mess up other tests)
                var dbContext = _factory.Services.GetRequiredService<RosterDbContext>();
                var student = await dbContext.Student.FindAsync(studentDto.Id);
                dbContext.Student.Remove(student);
                await dbContext.SaveChangesAsync();
            }
        }

        [Theory]
        [InlineData(1, "John Doe", 1, HttpStatusCode.Conflict)]  // Id already exists
        [InlineData(3, null, 1, HttpStatusCode.BadRequest)]      // missing (null) Name
        [InlineData(3, "", 1, HttpStatusCode.BadRequest)]        // missing (empty) Name
        [InlineData(3, "John Doe", 2, HttpStatusCode.NotFound)]  // Class doesn't exist
        public async Task CreateStudent_ReturnsErrorCode(int id, string name, int classId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient();
            var studentDto = new StudentDTO
            {
                Id = id,
                Name = name,
                ClassId = classId
            };
            var content = new StringContent(JsonSerializer.Serialize(studentDto, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/students", content);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStudent_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();
            var studentDto = new StudentDTO
            {
                Id = 2,
                Name = "Jaine Dough",
                ClassId = 1
            };
            var content = new StringContent(JsonSerializer.Serialize(studentDto, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PutAsync("/students/2", content);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Theory]
        [InlineData(2, 999, "Jaine Dough", 1, HttpStatusCode.BadRequest)] // url and dto Id's don't match
        [InlineData(2, 2, null,  1, HttpStatusCode.BadRequest)]           // missing (null) Name
        [InlineData(2, 2, "",  1, HttpStatusCode.BadRequest)]             // missing (empty) Name
        [InlineData(999, 999, "Jaine Dough", 1, HttpStatusCode.NotFound)] // Student not found
        [InlineData(2, 2, "Jaine Dough", 2, HttpStatusCode.NotFound)]     // Class not found
        public async Task UpdateStudent_ReturnsErrorCode(int urlId, int dtoId, string name, int classId, HttpStatusCode expectedStatusCode)
        {
            // Arrange
            var client = _factory.CreateClient();
            var studentDto = new StudentDTO
            {
                Id = dtoId,
                Name = name,
                ClassId = classId
            };
            var content = new StringContent(JsonSerializer.Serialize(studentDto, 
                new JsonSerializerOptions{IgnoreNullValues = true}), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PutAsync($"/students/{urlId}", content);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);

        }

        [Fact]
        public async Task DeleteStudent_ReturnsSuccessAndStudent()
        {
            // Arrange
            var client = _factory.CreateClient();
            var dbContext = _factory.Services.GetRequiredService<RosterDbContext>();
            var student = new Student
            {
                Id = 4,
                Name = "Jimmeny Cricket",
                Class = await dbContext.Class.FindAsync(1)
            };
            dbContext.Student.Add(student);
            await dbContext.SaveChangesAsync();

            // Act
            var response = await client.DeleteAsync("/students/4");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Content);
            var responseStudent = JsonSerializer.Deserialize<StudentDTO>(
                await response.Content.ReadAsStringAsync(),
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            Assert.NotNull(responseStudent);
            Assert.Equal(student.Id, responseStudent.Id);
            Assert.Equal(student.Name, responseStudent.Name);
            Assert.Equal(student.Class.Id, responseStudent.ClassId);
            Assert.Equal(student.Class.Teacher.Id, responseStudent.TeacherId);
            Assert.Equal(student.Class.Teacher.School.Id, responseStudent.SchoolId);
        }

        [Fact]
        public async Task DeleteStudent_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.DeleteAsync("/students/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
