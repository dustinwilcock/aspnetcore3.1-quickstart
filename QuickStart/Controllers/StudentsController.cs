using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickStart.Controllers.DTOs;
using QuickStart.Models;

namespace QuickStart.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly RosterDbContext _dbContext;

        public StudentsController(RosterDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// GET (Read all) /students
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerator<StudentDTO>>> GetAll()
        {
            var students = await _dbContext.Student.ToArrayAsync();
            return Ok(students.Select(s => s.ToDTO()));
        }

        /// <summary>
        /// GET (Read) /student/{id}
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentDTO>> Get(int id)
        {
            var student = await _dbContext.Student.FindAsync(id);

            if (student == null)
                return NotFound();

            return Ok(student.ToDTO());
        }

        /// <summary>
        /// POST (Create) /student
        /// </summary>
        /// <param name="studentDto"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<StudentDTO>> Create([FromBody] StudentDTO studentDto)
        {
            if (string.IsNullOrEmpty(studentDto.Name))
                return BadRequest();

            var @class = await _dbContext.Class.FindAsync(studentDto.ClassId);
            if (@class == null)
                return NotFound();

            var existingStudent = await _dbContext.Student.FindAsync(studentDto.Id);
            if (existingStudent != null)
                return Conflict();

            var studentToAdd = studentDto.ToModel(@class);
            _dbContext.Student.Add(studentToAdd);
            await _dbContext.SaveChangesAsync();
            var updatedStudentDto = studentToAdd.ToDTO();

            return CreatedAtAction(nameof(Get), new {id = studentDto.Id}, updatedStudentDto);
        }

        /// <summary>
        /// DELETE /student/{id}
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentDTO>> Delete(int id)
        {
            var student = await _dbContext.Student.FindAsync(id);
            if (student == null)
                return NotFound();

            _dbContext.Student.Remove(student);
            await _dbContext.SaveChangesAsync();
            return Ok(student.ToDTO());
        }

        /// <summary>
        /// PUT (Update) a Student by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="studentDto"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] StudentDTO studentDto)
        {
            if (studentDto.Id != id || string.IsNullOrEmpty(studentDto.Name))
                return BadRequest();

            var student = await _dbContext.Student.FindAsync(id);
            if (student == null)
                return NotFound();
            var @class = studentDto.ClassId != student.Class.Id
                ? await _dbContext.Class.FindAsync(id)
                : student.Class;
            if (@class == null)
                return NotFound();

            student.Update(studentDto, @class);
            await _dbContext.SaveChangesAsync();
            return NoContent();
        }
    }

    public static class StudentExtensions
    {
        public static Student ToModel(this StudentDTO studentDto, Class @class)
        {
            if (@class.Id != studentDto.ClassId) throw new NotSupportedException();
            return new Student
            {
                Id = studentDto.Id,
                Name = studentDto.Name,
                Class = @class
            };
        }

        public static void Update(this Student studentToUpdate, StudentDTO studentDto, Class @class)
        {
            if (studentDto.Id != studentToUpdate.Id) throw new NotSupportedException();
            studentToUpdate.Name = studentDto.Name;
            studentToUpdate.Class = @class;
        }

        public static StudentDTO ToDTO(this Student student)
        {
            return new StudentDTO
            {
                Id = student.Id,
                Name = student.Name,
                ClassId = student.Class.Id,
                TeacherId = student.Class.Teacher.Id,
                SchoolId = student.Class.Teacher.School.Id
            };
        }
    }
}
