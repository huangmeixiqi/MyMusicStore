
--创建流水号的函数
 ALTER function [dbo].[GetPurchaserSerialNo]()
 returns nvarchar(12)
 as
 begin
   declare @purchaseSerials nvarchar(12)
   declare @date nvarchar(8)
   declare @maxPurchaseSerials nvarchar(12)

   --取今天的日期 并转换成格式yyyymmdd
   set @date = convert(nvarchar,getdate(),112)

   --查询出今天最后一条申请的流水号
   select @maxPurchaseSerials=Max(Serialnumber) from Tbl_Purchase where Serialnumber like @date+'%'

   --判断今天是否有流水
   if(@maxPurchaseSerials is null)
       --今天没有流水，从0001开始
       set @purchaseSerials = @date+'0001'
   else
       begin
	      set @purchaseSerials =@date + right('0000'+ cast(convert(int,right(@maxPurchaseSerials,4))+1 as nvarchar(4)),4)
	   end

   return @purchaseSerials
end