using Application.Common;
using Application.Common.Interfaces;
using Application.Options;
using Application.VaccineCredential.Queries.GetVaccineCredential;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Infrastructure
{
    public class CredentialCreator : ICredentialCreator
    {

        private readonly KeySettings _keySettings;
        private readonly IJwtSign _jwtSign;

        public CredentialCreator(KeySettings keySettings, IJwtSign jwtSign)
        {
            _keySettings = keySettings;
            _jwtSign = jwtSign;
        }
        public Vci GetCredential(Vc vc)
        {
            var data = new Vci()
            {
                vc = vc,
                iss = _keySettings.Issuer,
                nbf = _jwtSign.ToUnixTimestamp(DateTime.Now)
            };
            return data;
        }        

        public GoogleWallet GetGoogleCredential(Vci cred, string shc)
        {
            var p = (Patient)cred.vc.credentialSubject.fhirBundle.Entry[0].Resource;
            var patientDetail = new PatientDetails()
            {
                dateOfBirth = p.BirthDate,
                identityAssuranceLevel = "IAL1.4",
                patientName = $"{ p.Name[0].GivenElement[0].Value} {p.Name[0].Family}"
            };

            var vaccinationRecords = new List<VaccinationRecord>();


            for (int inx = 1; inx < cred.vc.credentialSubject.fhirBundle.Entry.Count; inx++)
            {
                var dose = (Immunization)cred.vc.credentialSubject.fhirBundle.Entry[inx].Resource;
                var lotNumber = dose.LotNumber;
                if (string.IsNullOrWhiteSpace(lotNumber)) { lotNumber = null; }

                var vaccinationRecord = new VaccinationRecord()
                {
                    code = dose.VaccineCode.Coding[0].Code,
                    doseDateTime = dose.Occurrence.ToString(),
                    doseLabel = "Dose",
                    lotNumber = lotNumber,
                    manufacturer = Utils.VaccineTypeNames.GetValueOrDefault(dose.VaccineCode.Coding[0].Code),
                    description = Utils.VaccineTypeNames.GetValueOrDefault(dose.VaccineCode.Coding[0].Code)
                };

                vaccinationRecords.Add(vaccinationRecord);
            }



            var vaccinationDetail = new VaccinationDetails()
            {
                vaccinationRecord = vaccinationRecords
            };

            var logo = new Logo()
            {
                sourceUri = new SourceUri()
                {
                    description = "State of California",
                    uri = _keySettings.GoogleWalletLogo
                }
            };

            var cardObject = new CovidCardObject()
            {
                id = _keySettings.GoogleIssuerId + $".{Guid.NewGuid()}",
                issuerId = _keySettings.GoogleIssuerId,
                cardColorHex = "#FFFFFF",
                logo = logo,
                patientDetails = patientDetail,
                title = "COVID-19 Vaccination Card",
                vaccinationDetails = vaccinationDetail,
                barcode = new Barcode
                {
                    type = "qrCode",//Enum.GetName(typeof(BarcodeType), BarcodeType.QR_CODE),
                    value = shc,
                    
                    renderEncoding = Enum.GetName(typeof(BarcodeRenderEncoding), BarcodeRenderEncoding.UTF_8),
                    alternateText = ""
                }
            };

            var cardObjects = new List<CovidCardObject>
            {
                cardObject
            };

            var data = new GoogleWallet()
            {
                iss = _keySettings.GoogleIssuer,
                iat = _jwtSign.ToUnixTimestamp(DateTime.Now),
                aud = "google",
                typ = "savetogooglepay",
                origins = new List<object>(),
                payload = new Payload() { covidCardObjects = cardObjects }
            };
            return data;
        }
    }
}
