function [KV_injection_set]=single_bus_voltage_correction_popagation(current_bus_number,...
    injection_line_number_set,accurate_line_number,KV_accurate,calibration_history,line_estimation_results)

inaccurate_line_num = size(injection_line_number_set,1)-1;
inaccurate_injection_lines_set = [];%injection_line_number_set(injection_line_number_set(:,1)~=accurate_line_number,:);
KV_injection_set = [];%zeros(inaccurate_line_num,2);% [line_number KV]
% KV_injection_set(:,1) = inaccurate_injection_lines_set(:,1);

% V_leftside_set, V_rightside_set 
V_leftside_set = [];
for j=1:(size(injection_line_number_set,1))
    current_line_number = injection_line_number_set(j,1);
    temp_line_name=['line_' ,num2str(current_line_number), '_measured_positive_sequence.mat'];
%     temp_line_name=['line_' ,num2str(current_line_number), '_true_positive_sequence.mat'];
    temp_all_data_struct = load(temp_line_name);
    temp_all_data = temp_all_data_struct.VI_measurement_set;
%     temp_all_data = temp_all_data_struct.VI_true_set;
    
    if injection_line_number_set(j,2) == 0
        V_complex_temp = temp_all_data(:,1);
    else
        V_complex_temp = temp_all_data(:,3);
    end
       
    if current_line_number == accurate_line_number
        V_rightside_set = V_complex_temp;
    else
        if ~(~isempty(calibration_history) && sum(find(calibration_history(:,1)==current_line_number)))            
            V_leftside_set = [V_leftside_set, V_complex_temp];
            inaccurate_injection_lines_set = [inaccurate_injection_lines_set; current_line_number];
        end
    end
end

V_true = V_rightside_set.*KV_accurate;

for i = 1:size(V_leftside_set,2)
    current_KV_set = V_true./V_leftside_set(:,i);
    estimation_results(i,2) = mean(current_KV_set);
end
estimation_results(:,1) = inaccurate_injection_lines_set;

KV_injection_set = estimation_results;

end