using Clinic_Business;
using Clinic_Business.Entities;
using Clinic_Business.Interfaces;
using Clinic_Management_System.DTOs.Models.Medicines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Medicines")]
    [ApiController]
    public class MedicinesController : ControllerBase
    {
        private readonly IMedicineRepository _medicineRepository;

        public MedicinesController(IMedicineRepository medicineRepository)
        {
            _medicineRepository = medicineRepository;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetMedicines")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<MedicineDTO>>> GetMedicines([Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            List<MedicineDTO> medicinesList = (await _medicineRepository.FindAllAsync(page, perPage))
                .Select(medicine => new MedicineDTO
                {
                    Id = medicine.MedicineID ?? 0,
                    Genericname = medicine.Genericname,
                    Brandname = medicine.Brandname ?? string.Empty,
                    Form = medicine.Form,
                    Strength = medicine.Strength,
                    RxCUI = medicine.RxCUI,
                }).ToList();

            return Ok(medicinesList);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetMedicineByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MedicineDTO>> GetMedicineByID([Range(1, int.MaxValue)] int id)
        {
            clsMedicine? medicine = await _medicineRepository.FindByIdAsync(id);
            if (medicine == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Medicine with id {id} not found");

            MedicineDTO medicineDTO = new MedicineDTO
            {
                Id = id,
                Brandname = medicine.Brandname,
                Genericname = medicine.Genericname,
                Form = medicine.Form,
                Strength = medicine.Strength,
                RxCUI = medicine.RxCUI,
            };


            return Ok(medicineDTO);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost(Name = "AddMedicine")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult<MedicineDTO> AddMedicine(CreateMedicineDTO medicineDTO)
        {
            clsMedicine newMedicine = new clsMedicine();
            newMedicine.Genericname = medicineDTO.Genericname;
            newMedicine.Brandname = medicineDTO.Brandname;
            newMedicine.Form = medicineDTO.Form;
            newMedicine.Strength = medicineDTO.Strength;
            newMedicine.RxCUI = medicineDTO.RxCUI;

            if (newMedicine.Save())
            {
                MedicineDTO newMedicineDTO = new MedicineDTO
                {
                    Id = newMedicine.MedicineID,
                    RxCUI = medicineDTO.RxCUI,
                    Brandname = medicineDTO.Brandname,
                    Genericname = medicineDTO.Genericname,
                    Form = medicineDTO.Form,
                    Strength = medicineDTO.Strength,
                };
                return CreatedAtRoute("GetMedicineByID", new { id = newMedicine.MedicineID }, newMedicineDTO);
            }

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The medicine could not be saved. Check RxCUI, generic name, form and strength.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}", Name = "UpdateMedicine")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> UpdateMedicine(int id, UpdateMedicineDTO updatedMedicine)
        {
            clsMedicine? medicine = await _medicineRepository.FindByIdAsync(id);
            if (medicine == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Medicine with id {id} not found");

            medicine.Genericname = updatedMedicine.Genericname;
            medicine.Brandname = updatedMedicine.Brandname;
            medicine.Form = updatedMedicine.Form;
            medicine.Strength = updatedMedicine.Strength;

            if (medicine.Save())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The medicine could not be saved. Check RxCUI, generic name, form and strength.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeleteMedicine")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<ActionResult> DeleteMedicine([Range(1, int.MaxValue)] int id)
        {
            clsMedicine? medicine = await _medicineRepository.FindByIdAsync(id);
            if (medicine == null)
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    detail: $"Medicine with id {id} not found");

            if (await _medicineRepository.DeleteAsync(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                detail: "The medicine could not be deleted.");
        }
    }
}