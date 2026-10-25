using Clinic_Business.Common;
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
        private readonly IPrescriptionItemRepository _prescriptionItemRepository;

        public MedicinesController(
            IMedicineRepository medicineRepository,
            IPrescriptionItemRepository prescriptionItemRepository)
        {
            _medicineRepository = medicineRepository;
            _prescriptionItemRepository = prescriptionItemRepository;
        }

        [Authorize(Roles = "Admin,Staff,Doctor")]
        [HttpGet(Name = "GetMedicines")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<MedicineDTO>>> GetMedicines(string? name, [Range(1, int.MaxValue)] int page = 1, [Range(1, 10)] int perPage = 10)
        {
            PagedResult<clsMedicine> medicinesPage = await _medicineRepository.FindAllAsync(name, page, perPage);

            List<MedicineDTO> medicinesList = medicinesPage.Items
                .Select(medicine => new MedicineDTO
                {
                    Id = medicine.MedicineID ?? 0,
                    Genericname = medicine.Genericname,
                    Brandname = medicine.Brandname ?? string.Empty,
                    Form = medicine.Form,
                    Strength = medicine.Strength,
                    RxCUI = medicine.RxCUI,
                }).ToList();

            return Ok(new PagedResult<MedicineDTO>(medicinesList, medicinesPage.Page, medicinesPage.PerPage, medicinesPage.Total));
        }

        [Authorize(Roles = "Admin,Staff,Doctor")]
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
        public async Task<ActionResult<MedicineDTO>> AddMedicine(CreateMedicineDTO medicineDTO)
        {
            clsMedicine newMedicine = new clsMedicine();
            newMedicine.Genericname = medicineDTO.Genericname;
            newMedicine.Brandname = medicineDTO.Brandname;
            newMedicine.Form = medicineDTO.Form;
            newMedicine.Strength = medicineDTO.Strength;
            newMedicine.RxCUI = medicineDTO.RxCUI;

            if (await newMedicine.CreateAsync())
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

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The medicine could not be saved. Check RxCUI, generic name, form and strength.");
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
                return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Medicine with id {id} not found");

            medicine.Genericname = updatedMedicine.Genericname;
            medicine.Brandname = updatedMedicine.Brandname;
            medicine.Form = updatedMedicine.Form;
            medicine.Strength = updatedMedicine.Strength;

            if (await medicine.UpdateAsync())
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The medicine could not be saved. Check RxCUI, generic name, form and strength.");
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

            // Checked rather than caught: the foreign key would refuse anyway,
            // and that arrives as a 500 with nothing a caller can act on.
            if (await _prescriptionItemRepository.HasPrescriptionByMedicineIdAsync(id))
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity,
                    detail: "This medicine is on an existing prescription and cannot be deleted.");

            if (await _medicineRepository.DeleteAsync(id))
                return NoContent();

            return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, detail: "The medicine could not be deleted.");
        }
    }
}
