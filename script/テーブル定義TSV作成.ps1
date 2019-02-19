function main()
{
    cd $PSScriptRoot

    $excelFile = (Get-Item .\JLW基幹システムテーブル定義.xlsx).FullName
    $datetimeFormat = 'yyyy/MM/dd HH:mm:ss'

    try {

        $excel = New-Object -ComObject Excel.Application
        $excel.Visible = $false
        $excel.DisplayAlerts = $false
        $book = $excel.Workbooks.Open($excelFile)
        $indexSheet = $book.Sheets.Item("Index")
    
        ## テーブルの開設を取得
        Write-Host ('[{0}] テーブル情報取得開始' -f (Get-Date -Format $datetimeFormat))
        $row = 2
        $tables = @()
        #<#
        while(1) {
            $no = $indexSheet.Cells.Item($row,1).Text

            if([string]::IsNullOrEmpty($no)) {
                break
            }
            $tables += ([PSCustomObject]@{Name = $indexSheet.Cells.Item($row,2).Text; Detail = $indexSheet.Cells.Item($row,3).Text})
            $row++
        }
        Write-Host ('[{0}] テーブル情報取得終了' -f (Get-Date -Format $datetimeFormat))

        Write-Host ('[{0}] テーブル情報出力開始' -f (Get-Date -Format $datetimeFormat))
        $tables | select Name, Detail | Export-Csv -Delimiter "`t" -Path .\tables.tsv -Force -Encoding utf8 -NoTypeInformation
        Write-Host ('[{0}] テーブル情報出力終了' -f (Get-Date -Format $datetimeFormat))
        #>

        $columns = @()
        Write-Host ('[{0}] カラム情報取得開始' -f (Get-Date -Format $datetimeFormat))
        $book.Worksheets | where {$s = $_; $tables.Where({$s.Name -eq $_.Name},'First').Count -gt 0} | foreach {
            $sheet = $_
            Write-Host ('[{0}] {1}' -f (Get-Date -Format $datetimeFormat), $sheet.Name)

            $row = 5
            if($sheet.Cells.Item($row - 1,1).Text -ne '№') {
                $row = 4
            }
            while(1) {
                $no = $sheet.Cells.Item($row,1).Text

                if([string]::IsNullOrEmpty($no)) {
                    break
                }

                $fieldId = $sheet.Cells.Item($row,5).Text
                if([string]::IsNullOrEmpty($fieldId)) {
                    $row++
                    continue
                }


                $columns += ([PSCustomObject]@{Name = ('{0}.{1}' -f $sheet.Name, $sheet.Cells.Item($row,5).Text); JPName = $sheet.Cells.Item($row,2).Text; Detail = (Convert-DetailText -detail1 $sheet.Cells.Item($row,16).Text -detail2 $sheet.Cells.Item($row,17).Text)})
                $row++
            }
        }
        Write-Host ('[{0}] カラム情報取得終了' -f (Get-Date -Format $datetimeFormat))
        Write-Host ('[{0}] カラム情報出力開始' -f (Get-Date -Format $datetimeFormat))
        $columns | select Name, JPName, Detail | Export-Csv -Delimiter "`t" -Path .\columns.tsv -Force -Encoding utf8 -NoTypeInformation
        Write-Host ('[{0}] カラム情報出力終了' -f (Get-Date -Format $datetimeFormat))

        $book.Close()
        $excel.Quit()    
     } finally {
        @($indexSheet,$sheet,$excel,$book) | foreach{[void][System.Runtime.InteropServices.Marshal]::FinalReleaseComObject($_)}
     }
}

function Convert-DetailText([string]$detail1, [string]$detail2) {
    $details1 = $detail1.Split("`r`n")
    $details2 = $detail2.Split("`r`n")
    $allDetails = ($details1 + $details2) | where { -not ([string]::IsNullOrEmpty($_)) }
    return ($allDetails -join '<br/>')
}



## main
main