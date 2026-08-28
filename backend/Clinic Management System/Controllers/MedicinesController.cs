using Clinic_Business;
using Clinic_Management_System.DTOs.Models.Medicines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Clinic_Management_System.Controllers
{
    [Route("api/Medicines")]
    [ApiController]
    public class MedicinesController : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpGet(Name = "GetMedicines")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<MedicineDTO> GetMedicines(int page = 1, int perPage = 10)
        {
            if (page < 1 || perPage < 1)
                return BadRequest("Invalid parameters");

            perPage = Math.Clamp(perPage, 1, 10);

            DataTable table = clsMedicine.FindAll(page, perPage);
            List<MedicineDTO> medicinesList = new List<MedicineDTO>();

            foreach (DataRow row in table.Rows)
            {
                medicinesList.Add(new MedicineDTO
                {
                    Id = row.Field<int>("MedicineID"),
                    Genericname = row.Field<string>("Genericname") ?? string.Empty,
                    Brandname = row.Field<string>("Brandname") ?? string.Empty,
                    Form = row.Field<string>("Form") ?? string.Empty,
                    Strength = row.Field<string>("Strength") ?? string.Empty,
                    RxCUI = row.Field<int>("RxCUI"),
                });
            }

            return Ok(medicinesList);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}", Name = "GetMedicineByID")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<MedicineDTO> GetMedicineByID(int id)
        {
            if (id < 1)
                return BadRequest("Invalid medicine id.");

            clsMedicine? medicine = clsMedicine.Find(id);
            if (medicine == null)
                return NotFound($"Medicine with id {id} not found!");

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
        public ActionResult<MedicineDTO> AddMedicine(CreateMedicineDTO medicineDTO)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            return BadRequest("Invalid medicine data.");
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}", Name = "UpdateMedicine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateMedicine(int id, UpdateMedicineDTO updatedMedicine)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid medicine data.");

            clsMedicine? medicine = clsMedicine.Find(id);
            if (medicine == null)
                return NotFound($"Medicine with id {id} not found.");

            medicine.Genericname = updatedMedicine.Genericname;
            medicine.Brandname = updatedMedicine.Brandname;
            medicine.Form = updatedMedicine.Form;
            medicine.Strength = updatedMedicine.Strength;

            if (medicine.Save())
                return Ok($"Medicine with id {id} updated successfully");

            return BadRequest("Invalid medicine data.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}", Name = "DeleteMedicine")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult DeleteMedicine(int id)
        {
            if (id < 1)
                return BadRequest("Invalid medicine id");

            clsMedicine? medicine = clsMedicine.Find(id);
            if (medicine == null)
                return NotFound($"Medicine with id {id} not found");

            if (clsMedicine.Delete(id))
                return Ok($"Medicine with id of {id} has been deleted.");

            return BadRequest("Invalid medicine id");
        }
    }
}
