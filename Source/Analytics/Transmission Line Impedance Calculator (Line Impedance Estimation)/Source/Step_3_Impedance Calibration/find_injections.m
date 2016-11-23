function [injection_line_number_set] = find_injections(bus_number,line_bus_info)
%% find all line numbers injected into the current bus
% line_bus_info = load('line_bus_number_info.mat','-ascii');
injection_line_number_set_temp = zeros(size(line_bus_info,1),2); %[line_number, bus_position] bus_position = 0-from_bus; bus_position = 1-to_bus
for i=1:size(line_bus_info,1)
    if line_bus_info(i,3) == bus_number
        injection_line_number_set_temp(i,1)=line_bus_info(i,1);
        injection_line_number_set_temp(i,2)=0;
    elseif line_bus_info(i,4) == bus_number
        injection_line_number_set_temp(i,1)=line_bus_info(i,1);
        injection_line_number_set_temp(i,2)=1;
    end    

end
injection_line_number_set = injection_line_number_set_temp(injection_line_number_set_temp(:,1)~=0,:);
end