function [line_number] = find_line_from_buses(start_bus_number, parent_bus_number, line_bus_number_info)
% line_bus_number_info=load('line_bus_number_info.mat','-ascii');
line_number = 0;
for i=1:size(line_bus_number_info,1)
    if ((line_bus_number_info(i,2)==start_bus_number) && (line_bus_number_info(i,3)==parent_bus_number))||...
            ((line_bus_number_info(i,3)==start_bus_number) && (line_bus_number_info(i,2)==parent_bus_number))
        line_number = line_bus_number_info(i,1);
        break;
    end
end
end