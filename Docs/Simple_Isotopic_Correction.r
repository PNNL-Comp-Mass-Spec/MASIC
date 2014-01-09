

##Corrected 114/117 ratio=ROUND(((A114-0.02*A115)/(A117-0.049* A116)),3)
##Corrected 115/117 ratio=ROUND(((A115-0.03*A116-0.063*A114)/(A117-0.049* A116)),3)
##Corrected 116/117 ratio=ROUND(((A116-0.04*A117-0.06*A115)/(A117-0.049* A116)),3)

sourceFileName = "Q_4p_S2_for_quantitation_noMis.txt"
outputFileName = paste(
                    paste(
                            strsplit(sourceFileName, "\\.")[[1]][1],
                            "_IC",
                            sep=""
                         ),
                            strsplit(sourceFileName, "\\.")[[1]][2],
                            sep="."
                      )

# read source table
sourceData = read.delim( sourceFileName, check.names = F)

# 114 ratio
A114 = sourceData[,4]
A115 = sourceData[,5]
A116 = sourceData[,6]
A117 = sourceData[,7]
sourceData[,4] = round(((A114-0.02*A115)/(A117-0.049* A116)),3)
sourceData[,5] = round(((A115-0.03*A116-0.063*A114)/(A117-0.049* A116)),3)
sourceData[,6] = round(((A116-0.04*A117-0.06*A115)/(A117-0.049* A116)),3)
sourceData[,7] = 1

write.table( sourceData, file= outputFileName, quote=F, sep='\t', na="", row.names=F)


