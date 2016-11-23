% Error Model

clear
clc
close all

bulk_raw_data = xlsread('VI_Measurement_All_345KV_Buses_Peak.csv');
% bulk_raw_data_struct = load('bulk_raw_data.mat');
% bulk_raw_data = bulk_raw_data_struct.bulk_raw_data;
save('bulk_raw_data.mat','bulk_raw_data');
row_num = size(bulk_raw_data,1);
%% Bus numbers
idx = find(~isnan(bulk_raw_data(1,:)));
bus_number_set = bulk_raw_data(1,idx)';

save('Bus_number_set_345KV.mat', 'bus_number_set');

%% Voltage
raw_data = zeros(1800,22);
j = 1;
for i = 2:4:row_num  
    idx = find(~isnan(bulk_raw_data(i,:)));
    temp_raw_data = bulk_raw_data(i,idx);
    raw_data(j,:) = temp_raw_data;
    
    indicator = ['Timestamp ' num2str(j) ' input complete.'];
    disp(indicator);
    j=j+1;
end

save('V_true_value_positive_sequence.mat','raw_data');

%% Current of lines 
raw_data_current = zeros(1800,22);
line_num = size(raw_data_current,2)/2;
line_bus_info_all_lines = zeros(line_num,3); %[from_bus to_bus]

j = 1;
for i = 3:4:row_num
    
    idx = find(~isnan(bulk_raw_data(i,:)));
    temp_row = bulk_raw_data(i,idx);    
    
    temp_line_bus_info = zeros;
    
    temp_current_data = zeros;
    
    l=1;
    for k=1:7:76
        temp_frombus_number = temp_row(1,k);
        temp_tobus_number = temp_row(1,k+1);
        temp_ID = temp_row(1,k+2);
        temp_frombus_IR = temp_row(1,k+3);
        temp_frombus_II = temp_row(1,k+4);
        temp_tobus_IR = temp_row(1,k+5);
        temp_tobus_II = temp_row(1,k+6);
        
        temp_frombus_I_complex = temp_frombus_IR+1i*temp_frombus_II;
        temp_tobus_I_complex = temp_tobus_IR+1i*temp_tobus_II;
        
        line_bus_info_all_lines(l,:) = [temp_frombus_number, temp_tobus_number, temp_ID];
        raw_data_current(j,(2*l-1):(2*l)) = [temp_frombus_I_complex, temp_tobus_I_complex];
        l=l+1;
    end   
    
    indicator = ['Timestamp ' num2str(j) ' input complete.'];
    disp(indicator);
    j=j+1;
end
save('I_true_value_positive_sequence.mat','raw_data_current');
save('line_bus_info_all_lines.mat','line_bus_info_all_lines');


%% Current of 2-winding transformers
raw_data_current_trn = zeros(1800,18);
line_num = size(raw_data_current_trn,2)/2;
line_bus_info_trn = zeros(line_num,4); %[from_bus to_bus]

j = 1;
for i = 4:4:row_num
   
    idx = find(~isnan(bulk_raw_data(i,:)));
    temp_row = bulk_raw_data(i,idx);    
    
    temp_line_bus_info = zeros;
    
    temp_current_data = zeros;
    
    l=1;
    for k=1:8:71
        temp_frombus_number = temp_row(1,k);
        temp_tobus_number = temp_row(1,k+1);
        temp_trn_ID = temp_row(1,k+2);
        temp_frombus_flag = temp_row(1,k+3);
        temp_frombus_IR = temp_row(1,k+4);
        temp_frombus_II = temp_row(1,k+5);
        temp_tobus_IR = temp_row(1,k+6);
        temp_tobus_II = temp_row(1,k+7);
        
        temp_frombus_I_complex = temp_frombus_IR+1i*temp_frombus_II;
        temp_tobus_I_complex = temp_tobus_IR+1i*temp_tobus_II;
        
        line_bus_info_trn(l,:) = [temp_frombus_number, temp_tobus_number, temp_trn_ID, temp_frombus_flag];
        raw_data_current_trn(j,(2*l-1):(2*l)) = [temp_frombus_I_complex, temp_tobus_I_complex];
        l=l+1;
    end   
    
    indicator = ['Timestamp ' num2str(j) ' input complete.'];
    disp(indicator);
    j=j+1;
end
save('I_true_value_positive_sequence_trn.mat','raw_data_current_trn');
save('line_bus_info_trn.mat','line_bus_info_trn');


%% Current of generators
raw_data_current_gen = zeros(1800,4);
line_num_gen = size(raw_data_current_gen,2);
line_bus_info_gen = zeros(line_num_gen,2); %[gen_bus, -1]

j = 1;
for i = 5:4:row_num    
    
    idx = find(~isnan(bulk_raw_data(i,:)));
    temp_row = bulk_raw_data(i,idx);
    
    temp_line_bus_info = zeros;    
    temp_current_data = zeros;
    
    l=1;
    for k=1:4:15
        temp_gen_bus_number = temp_row(1,k);
        
        temp_gen_IR = temp_row(1,k+2);
        temp_gen_II = temp_row(1,k+3);
        
        temp_gen_I_complex = temp_gen_IR+1i*temp_gen_II;
        
        line_bus_info_gen(l,:) = [temp_gen_bus_number, -1];
        raw_data_current_gen(j,l) = temp_gen_I_complex;
        l=l+1;
    end   
    
    indicator = ['Timestamp ' num2str(j) ' input complete.'];
    disp(indicator);
    j=j+1;
end
save('I_true_value_positive_sequence_gen.mat','raw_data_current_gen');
save('line_bus_info_gen.mat','line_bus_info_gen');
