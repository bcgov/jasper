import { CourtDocumentType, DocumentData } from "@/types/shared"
import { splunkLog } from "@/utils/utils"
import base64url from "base64url"
import { v4 as uuidv4 } from "uuid"

export default {
  openDocumentsPdf(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): void {
    const fileName = this.generateFileName(documentType, documentData).replace(
      /\//g,
      "_"
    )
    const isCriminal = documentType == CourtDocumentType.Criminal
    const documentId = documentData.documentId
      ? base64url(documentData.documentId)
      : documentData.documentId
    const correlationId = uuidv4()

    switch (documentType) {
      case CourtDocumentType.CSR:
        window.open(
          `${import.meta.env.BASE_URL}api/files/civil/court-summary-report/${
            documentData.appearanceId
          }/${encodeURIComponent(fileName)}?vcCivilFileId=${
            documentData.fileId
          }`
        )
        break
      case CourtDocumentType.ROP:
        window.open(
          `${
            import.meta.env.BASE_URL
          }api/files/criminal/record-of-proceedings/${
            documentData.partId
          }/${encodeURIComponent(fileName)}?profSequenceNumber=${
            documentData.profSeqNo
          }&courtLevelCode=${documentData.courtLevel}&courtClassCode=${
            documentData.courtClass
          }`
        )
        break
      default:
        this.openRequestedTab(
          `${
            import.meta.env.BASE_URL
          }api/files/document/${documentId}/${encodeURIComponent(
            fileName
          )}?isCriminal=${isCriminal}&fileId=${
            documentData.fileId
          }&CorrelationId=${correlationId}`,
          correlationId
        )
        break
    }
  },

  generateFileName(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): string {
    const locationAbbreviation = (
      documentData.location.match(/[A-Z]/g) || []
    ).join("")
    switch (documentType) {
      case CourtDocumentType.Civil:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.dateFiled}-${documentData.documentId}.pdf`
      case CourtDocumentType.ProvidedCivil:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.appearanceDate}-${documentData.partyName}.pdf`
      case CourtDocumentType.CSR:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.appearanceDate}.pdf`
      case CourtDocumentType.Criminal:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.courtClass}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.dateFiled}-${documentData.documentId}.pdf`
      case CourtDocumentType.ROP:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.courtClass}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.partId}.pdf`
      case CourtDocumentType.CivilZip:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-documents.zip`
      case CourtDocumentType.CriminalZip:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.courtClass}-${documentData.fileNumberText}-documents.zip`
      default:
        throw Error(`No file structure for type: ${documentType}`)
    }
  },

  openRequestedTab(url, correlationId) {
    const start = new Date()
    const startStr = start.toLocaleString("en-US", {
      timeZone: "America/Vancouver"
    })
    const startMsg = `Request Tracking - Frontend request to API - CorrelationId: ${correlationId} Start time: ${startStr}`
    //console.log(startMsg);
    splunkLog(startMsg)

    const windowObjectReference = window.open(url)
    if (windowObjectReference !== null) {
      const end = new Date()
      const endStr = start.toLocaleString("en-US", {
        timeZone: "America/Vancouver"
      })

      const duration = (end.getTime() - start.getTime()) / 1000
      const endMsg = `Request Tracking - API response received - CorrelationId: ${correlationId} End time: ${endStr} Duration: ${duration}s`

      // eslint-disable-next-line
      windowObjectReference.onload = (event) => {
        if (windowObjectReference.document.readyState === "complete") {
          //console.log(endMsg);
          splunkLog(endMsg)
        }
      }
    }
  }
}
