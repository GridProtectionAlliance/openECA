Z=[];
y=[];

% system information
AC_line_info_struct=load('AC_line_info.mat');
AC_line_info = AC_line_info_struct.AC_line_info;
BaseZ = (345000^2)/100000000;

for line_number = 1:10
    line_name=['line_' ,num2str(line_number), '_true_positive_sequence.mat'];
    VI_origin_struct=load(line_name);
    VI = VI_origin_struct.VI_true_set;
    
    V1 = VI(:,1);
    I1 = VI(:,2);
    V2 = VI(:,3);
    I2 = VI(:,4);
    
    Z = [Z; mean((V1.*V1-V2.*V2)./(I1.*V2-I2.*V1))/BaseZ];
    
    y = [y; mean((I1+I2)./(V1+V2))*BaseZ];
    
end

save('impedance_per_unit.mat','Z');
save('shunt_B_per_unit.mat','y');

AC_line_info(1:10,10) = Z;
y=1i*imag(y);
AC_line_info(1:10,11) = y;

save('AC_line_info_true_value_Zy.mat','AC_line_info');
