// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using JCCommon.Clients.FileServices;
// using Microsoft.Extensions.Configuration;
// using Scv.Api.Services.Files;

// namespace JCCommon.Models
// {
//     public class eDocument
//     {
//         /// <summary>
//         /// A string that can be used to name the file to which the document content can be saved
//         /// </summary>
//         public string Filename { get; private set; }

//         /// <summary>
//         /// response message, set if the Edocument was not returned successfully.
//         /// </summary>
//         public string ResponseMessage { get; private set; }

//         /// <summary>
//         /// The binary content of the document
//         /// </summary>
//         public byte[] Content { get; private set; }

//         /// <summary>
//         /// true if and only if there is at least one byte in the document content
//         /// </summary>
//         public bool HasContent { get { return Content != null && Content.Length > 0; } }

//         /// <summary>
//         /// Return all the PDF documents available for the set of files associated with a single order
//         /// </summary>
//         /// <param name="files">a list of files to retrieve documents</param>
//         /// <param name="fileServicesClient">web API to JC-Interface</param>
//         /// <param name="summary">document list summary is appended</param>
//         /// <param name="docFilter">if null, return all documents, if non-null return only those documents for which this function returns true (Document1 is passed for criminal files, Document3 for noncriminal files</param>
//         /// <returns>a pair where .Item1 is a list of the file's documents, and .Item2 is a textual summary of the list</returns>
//         public static async Task<List<eDocument>> GetDocuments(IEnumerable<IFile> files, FilesService fileServicesClient, StringBuilder summary, Func<Document1, Document3, bool> docFilter = null)
//         {
//             var eDocs = new List<eDocument>();
//             foreach (IFile file in files)
//             {
//                 var eDocFiles = await GetDocumentsForFile(file, fileServicesClient, summary, docFilter);
//                 eDocs.AddRange(eDocFiles);
//             }
//             return eDocs;
//         }

//         private static async Task<List<eDocument>> GetDocumentsForFile(IFile file, FileServicesClient fileServicesClient, StringBuilder summary, Func<Document1, Document3, bool> docFilter = null)
//         {
//             var eDocs = new List<eDocument>();
//             bool isCriminal = (file.CourtDivisionCd == "R");

//             var appearance = file.Appearances.FirstOrDefault(); // get one of the appearances (doesn't matter which one, the file content will be the same)
//             return await GetDocumentsForAppearance(appearance, fileServicesClient, isCriminal, summary, docFilter);
//         }

//         public static async Task<List<eDocument>> GetDocumentsForAppearance(IAppearance appearance, FileServicesClient fileServicesClient, Boolean isCriminal, StringBuilder summary, Func<Document1, Document3, bool> docFilter = null)
//         {
//             Func<string, string, string, string, Task<eDocument>> GetDoc = async (fileNumber, imageId, classification, description) =>
//             {
//                 var name = "DOC-" + System.Net.WebUtility.UrlEncode(imageId + "-" + description) + ".pdf"; // construct a safe name for the file that will contain the document
//                 summary.AppendFormat("{0}: {1} {2}, {3}", name, fileNumber, classification, description);
//                 eDocument eDoc = await GetDocument(imageId, isCriminal, fileServicesClient, summary);
//                 eDoc.Filename = name;
//                 summary.AppendLine();
//                 return eDoc;
//             };

//             var eDocs = new List<eDocument>();
//             if (appearance != null)
//             {
//                 if (isCriminal)
//                 {
//                     CriminalFileContent fc = await fileServicesClient.Files.Criminal.Filecontent.GetCriminalFileContent(null, null, null, appearance.JustinAppearanceId, null);
//                     IList<AccusedFile> criminalFiles = fc?.AccusedFile == null ? new List<AccusedFile>() : fc.AccusedFile;
//                     foreach (AccusedFile f in fc.AccusedFile)
//                     {
//                         foreach (Document1 d in f.Document)
//                         {
//                             if (d != null)
//                             {
//                                 if (docFilter == null || docFilter(d, null))
//                                 {
//                                     eDocs.Add(await GetDoc(f.FileNumber, d.ImageId, d.DocmClassification, d.DocmFormDsc));
//                                 }
//                             }
//                         }
//                     }
//                 }
//                 else
//                 {
//                     CivilFileContent fc = await fileServicesClient.Files.Civil.CivilFilecontent.GetCivilFileContent(null, null, null, appearance.CeisAppearanceId, null);
//                     IList<CivilFile> civilFiles = fc?.CivilFile == null ? new List<CivilFile>() : fc.CivilFile;
//                     foreach (CivilFile f in fc.CivilFile)
//                     {
//                         foreach (Document3 d in f.Document)
//                         {
//                             if (d != null)
//                             {
//                                 if (docFilter == null || docFilter(null, d))
//                                 {
//                                     eDocs.Add(await GetDoc(f.FileNumber, d.CivilDocumentId, d.DocumentTypeCd, d.DocumentTypeDescription));
//                                 }
//                             }
//                         }
//                     }
//                 }
//             }
//             return eDocs;
//         }
//     }
// }