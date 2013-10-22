﻿using BForms.Mvc;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BForms.Utilities
{
    public static class ExcelHelpers
    {
        public static MemoryStream ToExcel<T>(this IEnumerable<T> items, string sheetName) where T : class
        {
            MemoryStream memoryStream = new MemoryStream();

            // Create the spreadsheet on the MemoryStream
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook);

            spreadsheetDocument.Fill(items, sheetName);

            // Close the document.
            spreadsheetDocument.Close();

            return memoryStream;
        }

        private static void Fill<T>(this SpreadsheetDocument spreadsheetDocument, IEnumerable<T> items, string sheetName) where T : class
        {
            // Add a WorkbookPart to the document.
            WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();

            // Add a WorksheetPart to the WorkbookPart.
            WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();

            var ws = new Worksheet();

            ws.Fill(items);

            worksheetPart.Worksheet = ws;
            worksheetPart.Worksheet.Save();

            // Add Sheets to the Workbook.
            Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild<Sheets>(new Sheets());

            // Append a new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = sheetName };
            sheets.Append(sheet);

            workbookpart.Workbook.Save();
        }

        private static void Fill<T>(this Worksheet worksheet, IEnumerable<T> items) where T : class
        {
            if (items == null || !items.Any()) return;

            var sheetData = new SheetData();

            var columns = new List<string>();

            Columns exColumns = new Columns();

            var headerRow = new Row();

            var type = typeof(T);

            var index = 0;

            // Create header based on DisplayAttribute and BsGridColumnAttribute
            foreach (var property in type.GetProperties())
            {
                BsGridColumnAttribute columnAttr = null;

                if (ReflectionHelpers.TryGetAttribute(property, out columnAttr))
                {
                    if (columnAttr.Usage != Models.BsGridColumnUsage.Html)
                    {
                        index++;

                        var width = columnAttr.Width;

                        string displayName = null;
                        DisplayAttribute displayAttribute = null;

                        if (ReflectionHelpers.TryGetAttribute(property, out displayAttribute))
                        {
                            displayName = displayAttribute.GetName();
                        }
                        else
                        {
                            displayName = property.Name;
                        }

                        columns.Add(property.Name);

                        exColumns.Append(CreateColumn((UInt32)index, (UInt32)index, width * 10));

                        headerRow.AppendChild(CreateCell(displayName));
                    }
                }
            }

            sheetData.AppendChild(headerRow);

            // Create data table
            foreach (var item in items)
            {
                var row = new Row();

                foreach (var column in columns)
                {
                    var property = type.GetProperty(column);

                    var value = property.GetValue(item);

                    var strValue = value as string;

                    if (strValue != null)
                    {
                        row.AppendChild(CreateCell(strValue));
                    }
                    else
                    {
                        throw new Exception(column + " is not of type string");
                    }
                }

                sheetData.AppendChild(row);
            }

            worksheet.Append(exColumns);
            worksheet.Append(sheetData);
        }

        private static Run GetBoldStyle()
        {
            Stylesheet styleSheet = new Stylesheet();//workbook.WorkbookStylesPart.Stylesheet;

            //build the formatted header style
            UInt32Value headerFontIndex =
                CreateFont(
                    styleSheet,
                    "Arial",
                    12,
                    true,
                    System.Drawing.Color.White);
            //set the background color style
            UInt32Value headerFillIndex =
                CreateFill(
                    styleSheet,
                    System.Drawing.Color.SlateGray);
            //create the cell style by combining font/background
            UInt32Value headerStyleIndex =
                CreateCellFormat(
                    styleSheet,
                    headerFontIndex,
                    headerFillIndex,
                    null);

            Cell headerCell = CreateTextCell(
                        1,
                        1,
                        "qwe",
                        headerStyleIndex);

            Run run = new Run();
            RunProperties runProperties = new RunProperties();
            Bold bold = new Bold();

            runProperties.Append(bold);
            run.Append(runProperties);

            return run;
        }

        private static UInt32Value CreateFont(Stylesheet styleSheet, string fontName, Nullable<double> fontSize, bool isBold, System.Drawing.Color foreColor)
        {
            Font font = new Font();

            if (!string.IsNullOrEmpty(fontName))
            {
                FontName name = new FontName()
                {
                    Val = fontName
                };
                font.Append(name);
            }

            if (fontSize.HasValue)
            {
                FontSize size = new FontSize()
                {
                    Val = fontSize.Value
                };
                font.Append(size);
            }

            if (isBold == true)
            {
                Bold bold = new Bold();
                font.Append(bold);
            }

            if (foreColor != null)
            {
                Color color = new Color()
                {
                    Rgb = new HexBinaryValue()
                    {
                        Value =
                            System.Drawing.ColorTranslator.ToHtml(
                                System.Drawing.Color.FromArgb(
                                    foreColor.A,
                                    foreColor.R,
                                    foreColor.G,
                                    foreColor.B)).Replace("#", "")
                    }
                };
                font.Append(color);
            }
            styleSheet.Fonts.Append(font);
            UInt32Value result = styleSheet.Fonts.Count;
            styleSheet.Fonts.Count++;
            return result;
        }

        private static UInt32Value CreateFill(Stylesheet styleSheet, System.Drawing.Color fillColor)
        {
            Fill fill = new Fill(
                new PatternFill(
                    new ForegroundColor()
                    {
                        Rgb = new HexBinaryValue()
                        {
                            Value =
                            System.Drawing.ColorTranslator.ToHtml(
                                System.Drawing.Color.FromArgb(
                                    fillColor.A,
                                    fillColor.R,
                                    fillColor.G,
                                    fillColor.B)).Replace("#", "")
                        }
                    })
                {
                    PatternType = PatternValues.Solid
                }
            );
            styleSheet.Fills.Append(fill);

            UInt32Value result = styleSheet.Fills.Count;
            styleSheet.Fills.Count++;
            return result;
        }

        private static UInt32Value CreateCellFormat(Stylesheet styleSheet, UInt32Value fontIndex, UInt32Value fillIndex, UInt32Value numberFormatId)
        {
            CellFormat cellFormat = new CellFormat();

            if (fontIndex != null)
                cellFormat.FontId = fontIndex;

            if (fillIndex != null)
                cellFormat.FillId = fillIndex;

            if (numberFormatId != null)
            {
                cellFormat.NumberFormatId = numberFormatId;
                cellFormat.ApplyNumberFormat = BooleanValue.FromBoolean(true);
            }

            styleSheet.CellFormats.Append(cellFormat);

            UInt32Value result = styleSheet.CellFormats.Count;
            styleSheet.CellFormats.Count++;
            return result;
        }

        private static Cell CreateTextCell(int columnIndex, int rowIndex, object cellValue, Nullable<uint> styleIndex)
        {
            Cell cell = new Cell();

            cell.DataType = CellValues.InlineString;
            cell.CellReference = GetColumnName(columnIndex) + rowIndex;

            if (styleIndex.HasValue)
                cell.StyleIndex = styleIndex.Value;

            InlineString inlineString = new InlineString();
            Text t = new Text();

            t.Text = cellValue.ToString();
            inlineString.AppendChild(t);
            cell.AppendChild(inlineString);

            return cell;
        }

        private static Cell CreateValueCell(int columnIndex, int rowIndex, object cellValue, Nullable<uint> styleIndex)
        {
            Cell cell = new Cell();
            cell.CellReference = GetColumnName(columnIndex) + rowIndex;
            CellValue value = new CellValue();
            value.Text = cellValue.ToString();

            if (styleIndex.HasValue)
                cell.StyleIndex = styleIndex.Value;

            cell.AppendChild(value);

            return cell;
        }

        private static string GetColumnName(int columnIndex)
        {
            int dividend = columnIndex;
            string columnName = String.Empty;
            int modifier;

            while (dividend > 0)
            {
                modifier = (dividend - 1) % 26;
                columnName =
                    Convert.ToChar(65 + modifier).ToString() + columnName;
                dividend = (int)((dividend - modifier) / 26);
            }

            return columnName;
        }

        private static Cell CreateCell(string name)
        {
            Cell cell = new Cell();
            cell.DataType = CellValues.String;
            cell.CellValue = new CellValue(name);
            //var run = GetBoldStyle();
            //run.Append(new Text(name));
            //var cellValue = new CellValue();
            //cellValue.Append(run);
            //cell.Append(cellValue);
            return cell;
        }

        private static Column CreateColumn(UInt32 startIndex, UInt32 endIndex, double width)
        {
            Column column;
            column = new Column();
            column.Min = startIndex;
            column.Max = endIndex;
            column.Width = width;
            column.CustomWidth = true;
            return column;
        }

        private static void Test()
        {
            //if (DateTime.TryParse(obj.ToString(), out dateValue))
            //{
            //    styleIndex = _dateStyleId;
            //    dataCell = CreateValueCell(i + 1, rowIndex, dateValue.ToOADate().ToString(), styleIndex);
            //}
            //else if (int.TryParse(obj.ToString(), out intValue))
            //{
            //    styleIndex = _numberStyleId;
            //    dataCell = CreateValueCell(i + 1, rowIndex, intValue, styleIndex);
            //}
            //else if (Double.TryParse(obj.ToString(), out doubleValue))
            //{
            //    styleIndex = _doubleStyleId;
            //    dataCell = CreateValueCell(i + 1, rowIndex, doubleValue, styleIndex);
            //}
            //else
            //{
            //    //assume the value is string, use the InlineString value type...
            //    dataCell = CreateTextCell(i + 1, rowIndex, dataRow[i], null);
            //}
        }
        
    }
}